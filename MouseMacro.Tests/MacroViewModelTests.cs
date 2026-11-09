using System.Windows;
using MouseMacro.Models;
using MouseMacro.Services;
using MouseMacro.ViewModels;

namespace MouseMacro.Tests;

public sealed class MacroViewModelTests : IDisposable
{
    private readonly string _tempFile;
    private readonly SettingsService _settingsService;

    public MacroViewModelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"mousemacro_macro_{Guid.NewGuid():N}.json");
        _settingsService = new SettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private MacroViewModel CreateViewModel(AppSettings? settings = null) =>
        new(settings ?? new AppSettings(),
            _settingsService,
            new FakeMouseHookService(),
            new FakeMouseInputService(),
            new FakeDialogService(),
            () => Rect.Empty);

    [Fact]
    public void ParseEvents_ParsesValidLines()
    {
        var text = "MouseMacro Recording v1\n# TimestampMs;Type;X;Y;Data\n100;Move;10;20;0\n200;LeftDown;30;40;0";

        var events = MacroViewModel.ParseEvents(text);

        Assert.Equal(2, events.Count);
        Assert.Equal(new RecordedMouseEvent(100, MouseEventType.Move, 10, 20, 0), events[0]);
        Assert.Equal(new RecordedMouseEvent(200, MouseEventType.LeftDown, 30, 40, 0), events[1]);
    }

    [Fact]
    public void ParseEvents_SkipsHeaderCommentAndMalformedLines()
    {
        var text = "MouseMacro Recording v1\n# comment\n\n50;Move;1;2;0\ngarbage line\n1;2;3\n75;LeftUp;5;6;0";

        var events = MacroViewModel.ParseEvents(text);

        Assert.Equal(2, events.Count);
        Assert.Equal(50, events[0].TimestampMs);
        Assert.Equal(75, events[1].TimestampMs);
    }

    [Fact]
    public void ParseEvents_ReturnsEmpty_ForEmptyText()
    {
        Assert.Empty(MacroViewModel.ParseEvents(string.Empty));
    }

    [Fact]
    public void MacroText_Edit_ParsesIntoEventList()
    {
        var vm = CreateViewModel();

        vm.MacroText = "MouseMacro Recording v1\n# TimestampMs;Type;X;Y;Data\n0;Move;1;2;0\n100;LeftDown;3;4;0\n200;LeftUp;3;4;0";

        Assert.Equal(3, vm.EventCount);
        Assert.Equal("0.2s", vm.DurationText);
    }

    [Fact]
    public void SettingChange_PersistsToSettingsFile()
    {
        var vm = CreateViewModel();

        vm.PlaybackSpeedPercent = 175;
        vm.LoopPlayback = true;
        vm.CompressMouseMovements = false;

        var reloaded = _settingsService.Load();
        Assert.Equal(175, reloaded.Macro.PlaybackSpeedPercent);
        Assert.True(reloaded.Macro.LoopPlayback);
        Assert.False(reloaded.Macro.CompressMouseMovements);
    }

    [Fact]
    public void StopRecording_CompressesMouseMoves_WhenEnabled()
    {
        var settings = new AppSettings();
        settings.Macro.CompressMouseMovements = true;
        var vm = CreateViewModel(settings);

        // Load events: a run of moves between two clicks should collapse to first+last.
        vm.MacroText = string.Join('\n', new[]
        {
            "MouseMacro Recording v1",
            "# TimestampMs;Type;X;Y;Data",
            "0;Move;0;0;0",
            "10;Move;1;1;0",
            "20;Move;2;2;0",
            "30;Move;3;3;0",
            "40;LeftDown;3;3;0",
            "50;LeftUp;3;3;0"
        });
        Assert.Equal(6, vm.EventCount);

        // Trigger compression via the internal method used when recording stops / loading a file.
        vm.ApplyCompression();

        // Compression keeps first+last Move (0 and 30) plus the two click events = 4.
        Assert.Equal(4, vm.EventCount);
        Assert.Contains(vm.MacroText.Split('\n'), l => l.StartsWith("0;Move;0;0;0"));
        Assert.Contains(vm.MacroText.Split('\n'), l => l.StartsWith("30;Move;3;3;0"));
        Assert.DoesNotContain(vm.MacroText.Split('\n'), l => l.StartsWith("10;Move"));
        Assert.DoesNotContain(vm.MacroText.Split('\n'), l => l.StartsWith("20;Move"));
    }

    [Fact]
    public void ApplyCompression_DoesNothing_WhenDisabled()
    {
        var settings = new AppSettings();
        settings.Macro.CompressMouseMovements = false;
        var vm = CreateViewModel(settings);

        vm.MacroText = string.Join('\n', new[]
        {
            "MouseMacro Recording v1",
            "# TimestampMs;Type;X;Y;Data",
            "0;Move;0;0;0",
            "10;Move;1;1;0",
            "20;Move;2;2;0",
            "40;LeftDown;3;3;0"
        });

        vm.ApplyCompression();

        Assert.Equal(4, vm.EventCount);
    }

    [Fact]
    public void ToDownType_MapsButtons()
    {
        Assert.Equal(MouseEventType.LeftDown, MacroViewModel.ToDownType(MouseButtonKind.Left));
        Assert.Equal(MouseEventType.RightDown, MacroViewModel.ToDownType(MouseButtonKind.Right));
        Assert.Equal(MouseEventType.MiddleDown, MacroViewModel.ToDownType(MouseButtonKind.Middle));
    }

    [Fact]
    public void ToUpType_MapsButtons()
    {
        Assert.Equal(MouseEventType.LeftUp, MacroViewModel.ToUpType(MouseButtonKind.Left));
        Assert.Equal(MouseEventType.RightUp, MacroViewModel.ToUpType(MouseButtonKind.Right));
        Assert.Equal(MouseEventType.MiddleUp, MacroViewModel.ToUpType(MouseButtonKind.Middle));
    }

    [Fact]
    public void FormatDuration_Formats()
    {
        Assert.Equal("0.0s", MacroViewModel.FormatDuration(0));
        Assert.Equal("1.5s", MacroViewModel.FormatDuration(1500));
        Assert.Equal("7.2s", MacroViewModel.FormatDuration(7200));
    }
}
