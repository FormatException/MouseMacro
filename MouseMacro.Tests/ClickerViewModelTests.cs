using MouseMacro.Models;
using MouseMacro.Services;
using MouseMacro.ViewModels;

namespace MouseMacro.Tests;

public sealed class ClickerViewModelTests : IDisposable
{
    private readonly string _tempFile;
    private readonly SettingsService _settingsService;

    public ClickerViewModelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"mousemacro_clicker_{Guid.NewGuid():N}.json");
        _settingsService = new SettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private ClickerViewModel CreateViewModel(AppSettings? settings = null) =>
        new(settings ?? new AppSettings(), _settingsService, new FakeMouseInputService());

    [Fact]
    public void Defaults_ClicksPerSecond_Is10000()
    {
        var vm = CreateViewModel();
        Assert.Equal(10000, vm.ClicksPerSecond);
    }

    [Fact]
    public void Defaults_SelectedButton_IsLeft()
    {
        var vm = CreateViewModel();
        Assert.Equal("Left", vm.SelectedButton);
    }

    [Fact]
    public void InvalidButton_FromSettings_FallsBackToLeft()
    {
        var settings = new AppSettings();
        settings.Clicker.MouseButton = "NotAButton";

        var vm = CreateViewModel(settings);

        Assert.Equal("Left", vm.SelectedButton);
    }

    [Fact]
    public void ClicksPerSecondChange_PersistsToSettingsFile()
    {
        var vm = CreateViewModel();

        vm.ClicksPerSecond = 250;

        Assert.Equal(250, _settingsService.Load().Clicker.ClicksPerSecond);
    }

    [Fact]
    public void SelectedButtonChange_PersistsToSettingsFile()
    {
        var vm = CreateViewModel();

        vm.SelectedButton = "Middle";

        Assert.Equal("Middle", _settingsService.Load().Clicker.MouseButton);
    }

    [Fact]
    public async Task Start_ClicksTheMouseInput_UntilStopped()
    {
        var mouse = new FakeMouseInputService();
        var settings = new AppSettings();
        settings.Clicker.ClicksPerSecond = 500;
        var vm = new ClickerViewModel(settings, _settingsService, mouse);

        vm.StartCommand.Execute(null); // do not await the click loop
        await WaitForAsync(() => mouse.ClickCount > 0);
        vm.StopCommand.Execute(null);
        await WaitForAsync(() => !vm.IsClicking);

        Assert.True(mouse.ClickCount > 0);
        Assert.Equal(MouseButtonKind.Left, mouse.LastButton);
        Assert.False(vm.IsClicking);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(20);
        }
    }
}
