using System.Windows;
using MouseMacro.Models;
using MouseMacro.Services;
using MouseMacro.ViewModels;

namespace MouseMacro;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private MouseHookService? _mouseHook;
    private KeyboardHookService? _keyboardHook;
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        AppSettings settings = settingsService.Load();

        var themeService = new ThemeService();
        themeService.ApplyTheme(settings.Theme);

        _mouseHook = new MouseHookService();
        _keyboardHook = new KeyboardHookService();
        var mouseInput = new MouseInputService();
        var dialogService = new DialogService();

        var mainWindow = new MainWindow();
        var macroViewModel = new MacroViewModel(settings, settingsService, _mouseHook, mouseInput, dialogService, mainWindow.GetScreenBounds);
        var clickerViewModel = new ClickerViewModel(settings, settingsService, mouseInput);
        _mainViewModel = new MainViewModel(settings, settingsService, macroViewModel, clickerViewModel);
        _mainViewModel.ThemeChanged += themeService.ApplyTheme;

        _keyboardHook.StartStopRequested += _mainViewModel.HandleStartStopHotkey;
        _keyboardHook.RecordToggleRequested += _mainViewModel.HandleRecordToggleHotkey;

        mainWindow.DataContext = _mainViewModel;
        MainWindow = mainWindow;
        mainWindow.Show();

        _mouseHook.Start();
        _keyboardHook.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.StopAll();
        _mouseHook?.Dispose();
        _keyboardHook?.Dispose();
        base.OnExit(e);
    }
}

