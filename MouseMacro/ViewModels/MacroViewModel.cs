using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseMacro.Models;
using MouseMacro.Services;

namespace MouseMacro.ViewModels;

public partial class MacroViewModel : ObservableObject
{
    private const string FileHeader = "MouseMacro Recording v1";
    private const string ColumnHeader = "# TimestampMs;Type;X;Y;Data";
    private const string FileFilter = "Mouse macro (*.txt)|*.txt|All files (*.*)|*.*";

    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly IMouseHookService _mouseHook;
    private readonly IMouseInputService _mouseInput;
    private readonly IDialogService _dialogService;
    private readonly Func<Rect> _getMainWindowBounds;
    private readonly List<RecordedMouseEvent> _events = new();

    private Stopwatch? _recordClock;
    private CancellationTokenSource? _playbackCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartPlaybackCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadFromFileCommand))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartPlaybackCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadFromFileCommand))]
    private bool _isPlaying;

    [ObservableProperty]
    private int _eventCount;

    [ObservableProperty]
    private string _durationText = "0.0s";

    [ObservableProperty]
    private string _macroText = string.Empty;

    [ObservableProperty]
    private int _currentEventIndex = -1;

    [ObservableProperty]
    private bool _recordMouseMovements;

    [ObservableProperty]
    private bool _compressMouseMovements;

    [ObservableProperty]
    private bool _loopPlayback;

    [ObservableProperty]
    private int _playbackSpeedPercent;

    public MacroViewModel(
        AppSettings settings,
        SettingsService settingsService,
        IMouseHookService mouseHook,
        IMouseInputService mouseInput,
        IDialogService dialogService,
        Func<Rect> getMainWindowBounds)
    {
        _settings = settings;
        _settingsService = settingsService;
        _mouseHook = mouseHook;
        _mouseInput = mouseInput;
        _dialogService = dialogService;
        _getMainWindowBounds = getMainWindowBounds;

        _recordMouseMovements = settings.Macro.RecordMouseMovements;
        _compressMouseMovements = settings.Macro.CompressMouseMovements;
        _loopPlayback = settings.Macro.LoopPlayback;
        _playbackSpeedPercent = settings.Macro.PlaybackSpeedPercent;

        _mouseHook.MouseMoved += OnMouseMoved;
        _mouseHook.MouseButtonDown += OnMouseButtonDown;
        _mouseHook.MouseButtonUp += OnMouseButtonUp;
        _mouseHook.MouseWheel += OnMouseWheel;
    }

    partial void OnRecordMouseMovementsChanged(bool value)
    {
        _settings.Macro.RecordMouseMovements = value;
        _settingsService.Save(_settings);
    }

    partial void OnCompressMouseMovementsChanged(bool value)
    {
        _settings.Macro.CompressMouseMovements = value;
        _settingsService.Save(_settings);
    }

    partial void OnLoopPlaybackChanged(bool value)
    {
        _settings.Macro.LoopPlayback = value;
        _settingsService.Save(_settings);
    }

    partial void OnPlaybackSpeedPercentChanged(int value)
    {
        _settings.Macro.PlaybackSpeedPercent = value;
        _settingsService.Save(_settings);
    }

    partial void OnMacroTextChanged(string value)
    {
        if (IsRecording || IsPlaying)
        {
            return;
        }

        var parsed = ParseEvents(value);
        _events.Clear();
        _events.AddRange(parsed);
        EventCount = _events.Count;
        DurationText = _events.Count > 0 ? FormatDuration(_events[^1].TimestampMs) : "0.0s";
        NotifyEventDependentCommands();
    }

    private void OnMouseMoved(int x, int y)
    {
        if (RecordMouseMovements)
        {
            RecordEvent(MouseEventType.Move, x, y, 0);
        }
    }

    private void OnMouseButtonDown(int x, int y, MouseButtonKind button) =>
        RecordEvent(ToDownType(button), x, y, 0);

    private void OnMouseButtonUp(int x, int y, MouseButtonKind button) =>
        RecordEvent(ToUpType(button), x, y, 0);

    private void OnMouseWheel(int x, int y, int delta) =>
        RecordEvent(MouseEventType.Wheel, x, y, delta);

    private void RecordEvent(MouseEventType type, int x, int y, int data)
    {
        if (!IsRecording || IsPlaying || _recordClock is null)
        {
            return;
        }

        // Ignore interaction with this app itself so clicking its buttons is not recorded.
        if (_getMainWindowBounds().Contains(x, y))
        {
            return;
        }

        _events.Add(new RecordedMouseEvent(_recordClock.ElapsedMilliseconds, type, x, y, data));
        EventCount = _events.Count;
        DurationText = FormatDuration(_events[^1].TimestampMs);
        MacroText = BuildMacroText();
    }

    private bool CanStartRecording() => !IsRecording && !IsPlaying;

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private void StartRecording()
    {
        _events.Clear();
        EventCount = 0;
        DurationText = "0.0s";
        MacroText = string.Empty;
        _recordClock = Stopwatch.StartNew();
        IsRecording = true;
    }

    private bool CanStopRecording() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private void StopRecording()
    {
        IsRecording = false;
        _recordClock?.Stop();
        ApplyCompression();
    }

    private bool CanStartPlayback() => !IsPlaying && !IsRecording && _events.Count > 0;

    [RelayCommand(CanExecute = nameof(CanStartPlayback))]
    private async Task StartPlayback()
    {
        IsPlaying = true;
        _playbackCts = new CancellationTokenSource();
        var token = _playbackCts.Token;
        var events = _events.ToArray();
        double speed = Math.Max(1, PlaybackSpeedPercent) / 100.0;
        bool loop = LoopPlayback;

        try
        {
            await Task.Run(() => PlaybackLoop(events, speed, loop, token), token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsPlaying = false;
            _playbackCts.Dispose();
            _playbackCts = null;
        }
    }

    private void PlaybackLoop(RecordedMouseEvent[] events, double speed, bool loop, CancellationToken token)
    {
        try
        {
            do
            {
                var clock = Stopwatch.StartNew();
                for (int i = 0; i < events.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    UpdateCurrentEventIndex(i);

                    if (!WaitUntil(clock, (long)(events[i].TimestampMs / speed), token))
                    {
                        return;
                    }

                    Dispatch(events[i]);
                }
            }
            while (loop && !token.IsCancellationRequested);
        }
        finally
        {
            UpdateCurrentEventIndex(-1);
        }
    }

    private void UpdateCurrentEventIndex(int index)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            CurrentEventIndex = index;
        }
        else
        {
            dispatcher.BeginInvoke(() => CurrentEventIndex = index);
        }
    }

    private static bool WaitUntil(Stopwatch clock, long targetMs, CancellationToken token)
    {
        while (true)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            long remaining = targetMs - clock.ElapsedMilliseconds;
            if (remaining <= 0)
            {
                return true;
            }

            if (remaining > 20)
            {
                Thread.Sleep((int)Math.Min(remaining - 10, 500));
            }
            else if (remaining > 2)
            {
                Thread.Sleep(1);
            }
            else
            {
                Thread.SpinWait(100);
            }
        }
    }

    private void Dispatch(RecordedMouseEvent mouseEvent)
    {
        switch (mouseEvent.Type)
        {
            case MouseEventType.Move:
                _mouseInput.MoveTo(mouseEvent.X, mouseEvent.Y);
                break;
            case MouseEventType.LeftDown:
                _mouseInput.ButtonDownAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Left);
                break;
            case MouseEventType.LeftUp:
                _mouseInput.ButtonUpAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Left);
                break;
            case MouseEventType.RightDown:
                _mouseInput.ButtonDownAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Right);
                break;
            case MouseEventType.RightUp:
                _mouseInput.ButtonUpAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Right);
                break;
            case MouseEventType.MiddleDown:
                _mouseInput.ButtonDownAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Middle);
                break;
            case MouseEventType.MiddleUp:
                _mouseInput.ButtonUpAt(mouseEvent.X, mouseEvent.Y, MouseButtonKind.Middle);
                break;
            case MouseEventType.Wheel:
                _mouseInput.Wheel(mouseEvent.Data);
                break;
        }
    }

    private bool CanStopPlayback() => IsPlaying;

    [RelayCommand(CanExecute = nameof(CanStopPlayback))]
    private void StopPlayback() => _playbackCts?.Cancel();

    private bool CanClear() => !IsRecording && !IsPlaying && _events.Count > 0;

    [RelayCommand(CanExecute = nameof(CanClear))]
    private void Clear()
    {
        _events.Clear();
        EventCount = 0;
        DurationText = "0.0s";
        MacroText = string.Empty;
        NotifyEventDependentCommands();
    }

    private bool CanSaveToFile() => !IsRecording && _events.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSaveToFile))]
    private void SaveToFile()
    {
        var path = _dialogService.ShowSaveFileDialog(FileFilter, "macro.txt");
        if (path is null)
        {
            return;
        }

        File.WriteAllText(path, BuildMacroText());
    }

    private bool CanLoadFromFile() => !IsRecording && !IsPlaying;

    [RelayCommand(CanExecute = nameof(CanLoadFromFile))]
    private void LoadFromFile()
    {
        var path = _dialogService.ShowOpenFileDialog(FileFilter);
        if (path is null)
        {
            return;
        }

        MacroText = File.ReadAllText(path);
        ApplyCompression();
    }

    internal void ApplyCompression()
    {
        if (!CompressMouseMovements || _events.Count == 0)
        {
            return;
        }

        var compressed = new List<RecordedMouseEvent>(_events.Count);
        var pendingMoves = new List<RecordedMouseEvent>();

        void FlushMoves()
        {
            if (pendingMoves.Count == 0)
            {
                return;
            }

            compressed.Add(pendingMoves[0]);
            if (pendingMoves.Count > 1)
            {
                compressed.Add(pendingMoves[^1]);
            }

            pendingMoves.Clear();
        }

        foreach (var mouseEvent in _events)
        {
            if (mouseEvent.Type == MouseEventType.Move)
            {
                pendingMoves.Add(mouseEvent);
            }
            else
            {
                FlushMoves();
                compressed.Add(mouseEvent);
            }
        }

        FlushMoves();

        if (compressed.Count == _events.Count)
        {
            return;
        }

        _events.Clear();
        _events.AddRange(compressed);
        EventCount = _events.Count;
        DurationText = FormatDuration(_events[^1].TimestampMs);
        MacroText = BuildMacroText();
    }

    private string BuildMacroText()
    {
        var builder = new StringBuilder(FileHeader).AppendLine().AppendLine(ColumnHeader);
        foreach (var mouseEvent in _events)
        {
            builder
                .Append(mouseEvent.TimestampMs).Append(';')
                .Append(mouseEvent.Type).Append(';')
                .Append(mouseEvent.X).Append(';')
                .Append(mouseEvent.Y).Append(';')
                .Append(mouseEvent.Data)
                .AppendLine();
        }

        return builder.ToString();
    }

    internal static List<RecordedMouseEvent> ParseEvents(string text)
    {
        var loaded = new List<RecordedMouseEvent>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0 || line == FileHeader || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split(';');
            if (parts.Length < 5
                || !long.TryParse(parts[0], out var timestampMs)
                || !Enum.TryParse<MouseEventType>(parts[1], out var type)
                || !int.TryParse(parts[2], out var x)
                || !int.TryParse(parts[3], out var y)
                || !int.TryParse(parts[4], out var data))
            {
                continue;
            }

            loaded.Add(new RecordedMouseEvent(timestampMs, type, x, y, data));
        }

        return loaded;
    }

    private void NotifyEventDependentCommands()
    {
        StartPlaybackCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
        SaveToFileCommand.NotifyCanExecuteChanged();
    }

    internal static MouseEventType ToDownType(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Right => MouseEventType.RightDown,
        MouseButtonKind.Middle => MouseEventType.MiddleDown,
        _ => MouseEventType.LeftDown
    };

    internal static MouseEventType ToUpType(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Right => MouseEventType.RightUp,
        MouseButtonKind.Middle => MouseEventType.MiddleUp,
        _ => MouseEventType.LeftUp
    };

    internal static string FormatDuration(long ms) => $"{ms / 1000.0:0.0}s";
}
