using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MouseMacro.Models;
using MouseMacro.Services;

namespace MouseMacro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private bool _isMacroSelected;

    [ObservableProperty]
    private bool _isClickerSelected;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _selectedTheme;

    public IReadOnlyList<string> Themes { get; } = new[] { "Dark", "Light" };

    public event Action<string>? ThemeChanged;

    public MacroViewModel MacroViewModel { get; }
    public ClickerViewModel ClickerViewModel { get; }

    public MainViewModel(
        AppSettings settings,
        SettingsService settingsService,
        MacroViewModel macroViewModel,
        ClickerViewModel clickerViewModel)
    {
        _settings = settings;
        _settingsService = settingsService;
        MacroViewModel = macroViewModel;
        ClickerViewModel = clickerViewModel;
        _selectedTheme = Themes.Contains(settings.Theme) ? settings.Theme : "Dark";

        MacroViewModel.PropertyChanged += OnChildPropertyChanged;
        ClickerViewModel.PropertyChanged += OnChildPropertyChanged;

        if (string.Equals(settings.SelectedMode, "Clicker", StringComparison.OrdinalIgnoreCase))
        {
            IsClickerSelected = true;
        }
        else
        {
            IsMacroSelected = true;
        }
    }

    partial void OnIsMacroSelectedChanged(bool value)
    {
        if (!value)
        {
            return;
        }

        CurrentView = MacroViewModel;
        _settings.SelectedMode = "Macro";
        _settingsService.Save(_settings);
        UpdateStatus();
    }

    partial void OnIsClickerSelectedChanged(bool value)
    {
        if (!value)
        {
            return;
        }

        CurrentView = ClickerViewModel;
        _settings.SelectedMode = "Clicker";
        _settingsService.Save(_settings);
        UpdateStatus();
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _settings.Theme = value;
        _settingsService.Save(_settings);
        ThemeChanged?.Invoke(value);
    }

    public void HandleStartStopHotkey()
    {
        // Always stop whatever is currently running, regardless of the selected view,
        // so switching views mid-operation doesn't stop the wrong thing.
        if (MacroViewModel.StopPlaybackCommand.CanExecute(null))
        {
            MacroViewModel.StopPlaybackCommand.Execute(null);
            return;
        }

        if (ClickerViewModel.StopCommand.CanExecute(null))
        {
            ClickerViewModel.StopCommand.Execute(null);
            return;
        }

        // Nothing running: start based on the selected view.
        if (IsMacroSelected)
        {
            if (MacroViewModel.StartPlaybackCommand.CanExecute(null))
            {
                MacroViewModel.StartPlaybackCommand.Execute(null);
            }
        }
        else if (ClickerViewModel.StartCommand.CanExecute(null))
        {
            ClickerViewModel.StartCommand.Execute(null);
        }
    }

    public void HandleRecordToggleHotkey()
    {
        // Stop an in-progress recording even if the user switched views.
        if (MacroViewModel.StopRecordingCommand.CanExecute(null))
        {
            MacroViewModel.StopRecordingCommand.Execute(null);
            return;
        }

        if (IsMacroSelected && MacroViewModel.StartRecordingCommand.CanExecute(null))
        {
            MacroViewModel.StartRecordingCommand.Execute(null);
        }
    }

    public void StopAll()
    {
        if (MacroViewModel.StopPlaybackCommand.CanExecute(null))
        {
            MacroViewModel.StopPlaybackCommand.Execute(null);
        }

        if (MacroViewModel.StopRecordingCommand.CanExecute(null))
        {
            MacroViewModel.StopRecordingCommand.Execute(null);
        }

        if (ClickerViewModel.StopCommand.CanExecute(null))
        {
            ClickerViewModel.StopCommand.Execute(null);
        }
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MacroViewModel.IsRecording)
            or nameof(MacroViewModel.IsPlaying)
            or nameof(ClickerViewModel.IsClicking))
        {
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        if (MacroViewModel.IsRecording)
        {
            StatusText = "Recording macro...";
            IsBusy = true;
        }
        else if (MacroViewModel.IsPlaying)
        {
            StatusText = "Playing macro...";
            IsBusy = true;
        }
        else if (ClickerViewModel.IsClicking)
        {
            StatusText = "Clicker running...";
            IsBusy = true;
        }
        else
        {
            StatusText = "Ready";
            IsBusy = false;
        }
    }
}
