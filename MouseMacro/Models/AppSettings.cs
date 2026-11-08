namespace MouseMacro.Models;

public sealed class AppSettings
{
    public string SelectedMode { get; set; } = "Macro";
    public string Theme { get; set; } = "Dark";
    public MacroSettings Macro { get; set; } = new();
    public ClickerSettings Clicker { get; set; } = new();
}

public sealed class MacroSettings
{
    public bool RecordMouseMovements { get; set; } = true;
    public bool CompressMouseMovements { get; set; } = true;
    public bool LoopPlayback { get; set; }
    public int PlaybackSpeedPercent { get; set; } = 100;
}

public sealed class ClickerSettings
{
    public double ClicksPerSecond { get; set; } = 10000;
    public string MouseButton { get; set; } = "Left";
}
