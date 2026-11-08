namespace MouseMacro.Models;

public enum MouseEventType
{
    Move,
    LeftDown,
    LeftUp,
    RightDown,
    RightUp,
    MiddleDown,
    MiddleUp,
    Wheel
}

public enum MouseButtonKind
{
    Left,
    Right,
    Middle
}

public readonly record struct RecordedMouseEvent(long TimestampMs, MouseEventType Type, int X, int Y, int Data);
