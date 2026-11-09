using MouseMacro.Models;
using MouseMacro.Services;

namespace MouseMacro.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"mousemacro_settings_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var service = new SettingsService(_tempFile);

        var settings = service.Load();

        Assert.Equal("Macro", settings.SelectedMode);
        Assert.Equal("Dark", settings.Theme);
        Assert.Equal(10000, settings.Clicker.ClicksPerSecond);
        Assert.True(settings.Macro.RecordMouseMovements);
        Assert.True(settings.Macro.CompressMouseMovements);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var service = new SettingsService(_tempFile);
        var settings = new AppSettings
        {
            SelectedMode = "Clicker",
            Theme = "Light",
            Macro = new MacroSettings { PlaybackSpeedPercent = 250, LoopPlayback = true },
            Clicker = new ClickerSettings { ClicksPerSecond = 500, MouseButton = "Right" }
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal("Clicker", loaded.SelectedMode);
        Assert.Equal("Light", loaded.Theme);
        Assert.Equal(250, loaded.Macro.PlaybackSpeedPercent);
        Assert.True(loaded.Macro.LoopPlayback);
        Assert.Equal(500, loaded.Clicker.ClicksPerSecond);
        Assert.Equal("Right", loaded.Clicker.MouseButton);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileIsCorrupt()
    {
        File.WriteAllText(_tempFile, "{ not valid json !!!");
        var service = new SettingsService(_tempFile);

        var settings = service.Load();

        Assert.Equal("Macro", settings.SelectedMode);
    }
}
