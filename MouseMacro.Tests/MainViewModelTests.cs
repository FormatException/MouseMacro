using System.Windows;
using MouseMacro.Models;
using MouseMacro.Services;
using MouseMacro.ViewModels;

namespace MouseMacro.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _tempFile;
    private readonly SettingsService _settingsService;

    public MainViewModelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"mousemacro_main_{Guid.NewGuid():N}.json");
        _settingsService = new SettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private MainViewModel CreateViewModel(AppSettings? settings = null)
    {
        settings ??= new AppSettings();
        var macro = new MacroViewModel(settings, _settingsService, new FakeMouseHookService(), new FakeMouseInputService(), new FakeDialogService(), () => Rect.Empty);
        var clicker = new ClickerViewModel(settings, _settingsService, new FakeMouseInputService());
        return new MainViewModel(settings, _settingsService, macro, clicker);
    }

    [Fact]
    public void Defaults_MacroViewSelected()
    {
        var vm = CreateViewModel();

        Assert.True(vm.IsMacroSelected);
        Assert.False(vm.IsClickerSelected);
        Assert.Same(vm.MacroViewModel, vm.CurrentView);
    }

    [Fact]
    public void SelectingClicker_SwitchesCurrentView_AndPersists()
    {
        var vm = CreateViewModel();

        vm.IsClickerSelected = true;

        Assert.Same(vm.ClickerViewModel, vm.CurrentView);
        Assert.Equal("Clicker", _settingsService.Load().SelectedMode);
    }

    [Fact]
    public void RespectsSavedClickerMode_OnStartup()
    {
        var settings = new AppSettings { SelectedMode = "Clicker" };

        var vm = CreateViewModel(settings);

        Assert.True(vm.IsClickerSelected);
        Assert.Same(vm.ClickerViewModel, vm.CurrentView);
    }

    [Fact]
    public void ThemeChange_Persists_AndRaisesEvent()
    {
        var vm = CreateViewModel();
        string? raised = null;
        vm.ThemeChanged += t => raised = t;

        vm.SelectedTheme = "Light";

        Assert.Equal("Light", raised);
        Assert.Equal("Light", _settingsService.Load().Theme);
    }

    [Fact]
    public void InvalidTheme_FromSettings_FallsBackToDark()
    {
        var settings = new AppSettings { Theme = "Neon" };

        var vm = CreateViewModel(settings);

        Assert.Equal("Dark", vm.SelectedTheme);
    }

    [Fact]
    public async Task StartStopHotkey_StartsClicker_WhenClickerSelected()
    {
        var vm = CreateViewModel();
        vm.IsMacroSelected = false;
        vm.IsClickerSelected = true;

        // Start without awaiting the (infinite) click loop.
        vm.HandleStartStopHotkey();
        await WaitForAsync(() => vm.ClickerViewModel.IsClicking);

        Assert.True(vm.ClickerViewModel.IsClicking);
        vm.StopAll();
    }

    [Fact]
    public async Task StartStopHotkey_StopsClicker_EvenWhenMacroViewSelected()
    {
        // Start the clicker, then switch to the Macro view; the hotkey must still stop the clicker.
        var vm = CreateViewModel();
        vm.IsMacroSelected = false;
        vm.IsClickerSelected = true;
        vm.HandleStartStopHotkey();
        await WaitForAsync(() => vm.ClickerViewModel.IsClicking);

        vm.IsClickerSelected = false;
        vm.IsMacroSelected = true;
        vm.HandleStartStopHotkey();
        await WaitForAsync(() => !vm.ClickerViewModel.IsClicking);

        Assert.False(vm.ClickerViewModel.IsClicking);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(20);
        }
    }

    [Fact]
    public void RecordToggleHotkey_DoesNothing_WhenClickerSelected()
    {
        var vm = CreateViewModel();
        // Mirror what the radio-button group does in the UI: only one mode selected.
        vm.IsMacroSelected = false;
        vm.IsClickerSelected = true;

        vm.HandleRecordToggleHotkey();

        Assert.False(vm.MacroViewModel.IsRecording);
    }

    [Fact]
    public void RecordToggleHotkey_StartsAndStopsRecording_WhenMacroSelected()
    {
        var vm = CreateViewModel();
        vm.IsMacroSelected = true;

        vm.HandleRecordToggleHotkey();
        Assert.True(vm.MacroViewModel.IsRecording);

        vm.HandleRecordToggleHotkey();
        Assert.False(vm.MacroViewModel.IsRecording);
    }

    [Fact]
    public void RecordToggleHotkey_StopsRecording_EvenWhenClickerSelected()
    {
        var vm = CreateViewModel();
        vm.IsMacroSelected = true;
        vm.HandleRecordToggleHotkey();
        Assert.True(vm.MacroViewModel.IsRecording);

        vm.IsMacroSelected = false;
        vm.IsClickerSelected = true;
        vm.HandleRecordToggleHotkey();

        Assert.False(vm.MacroViewModel.IsRecording);
    }
}
