using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseMacro.Models;
using MouseMacro.Services;

namespace MouseMacro.ViewModels;

public partial class ClickerViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly MouseInputService _mouseInput;
    private CancellationTokenSource? _clickerCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isClicking;

    [ObservableProperty]
    private double _clicksPerSecond;

    [ObservableProperty]
    private string _selectedButton;

    [ObservableProperty]
    private long _clicksPerformed;

    [ObservableProperty]
    private string _statusText = "Stopped";

    public IReadOnlyList<string> MouseButtons { get; } = new[] { "Left", "Right", "Middle" };

    public ClickerViewModel(AppSettings settings, SettingsService settingsService, MouseInputService mouseInput)
    {
        _settings = settings;
        _settingsService = settingsService;
        _mouseInput = mouseInput;

        _clicksPerSecond = settings.Clicker.ClicksPerSecond;
        _selectedButton = MouseButtons.Contains(settings.Clicker.MouseButton) ? settings.Clicker.MouseButton : "Left";
    }

    partial void OnClicksPerSecondChanged(double value)
    {
        _settings.Clicker.ClicksPerSecond = value;
        _settingsService.Save(_settings);
    }

    partial void OnSelectedButtonChanged(string value)
    {
        _settings.Clicker.MouseButton = value;
        _settingsService.Save(_settings);
    }

    private bool CanStart() => !IsClicking;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        double cps = ClicksPerSecond;
        if (double.IsNaN(cps) || double.IsInfinity(cps) || cps <= 0)
        {
            cps = 1;
        }
        else if (cps > 100000)
        {
            cps = 100000;
        }

        var button = Enum.TryParse<MouseButtonKind>(SelectedButton, out var parsed) ? parsed : MouseButtonKind.Left;

        IsClicking = true;
        StatusText = "Clicking...";
        ClicksPerformed = 0;
        _clickerCts = new CancellationTokenSource();
        var token = _clickerCts.Token;

        try
        {
            await Task.Run(() => ClickLoop(cps, button, token), token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsClicking = false;
            StatusText = "Stopped";
            _clickerCts.Dispose();
            _clickerCts = null;
        }
    }

    private bool CanStop() => IsClicking;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _clickerCts?.Cancel();

    private void ClickLoop(double clicksPerSecond, MouseButtonKind button, CancellationToken token)
    {
        timeBeginPeriod(1);
        try
        {
            double intervalTicks = Stopwatch.Frequency / clicksPerSecond;
            long start = Stopwatch.GetTimestamp();
            long count = 0;

            while (!token.IsCancellationRequested)
            {
                count++;
                long next = start + (long)(count * intervalTicks);
                _mouseInput.Click(button);

                if (count % 256 == 0)
                {
                    UpdateClicksPerformed(count);
                }

                while (!token.IsCancellationRequested)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (now >= next)
                    {
                        break;
                    }

                    double remainingMs = (next - now) * 1000.0 / Stopwatch.Frequency;
                    if (remainingMs > 2)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.SpinWait(50);
                    }
                }
            }

            UpdateClicksPerformed(count);
        }
        finally
        {
            timeEndPeriod(1);
        }
    }

    private void UpdateClicksPerformed(long count)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ClicksPerformed = count;
        }
        else
        {
            dispatcher.BeginInvoke(() => ClicksPerformed = count);
        }
    }

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
