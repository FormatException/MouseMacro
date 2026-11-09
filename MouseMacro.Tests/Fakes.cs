using MouseMacro.Models;
using MouseMacro.Services;

namespace MouseMacro.Tests;

/// <summary>No-op mouse input so tests never drive the real cursor.</summary>
public sealed class FakeMouseInputService : IMouseInputService
{
    public int ClickCount { get; private set; }
    public MouseButtonKind LastButton { get; private set; }

    public void MoveTo(int x, int y) { }
    public void ButtonDownAt(int x, int y, MouseButtonKind button) { }
    public void ButtonUpAt(int x, int y, MouseButtonKind button) { }
    public void Wheel(int delta) { }

    public void Click(MouseButtonKind button)
    {
        ClickCount++;
        LastButton = button;
    }
}

/// <summary>No-op mouse hook so tests never install a global hook.</summary>
public sealed class FakeMouseHookService : IMouseHookService
{
    public event Action<int, int>? MouseMoved;
    public event Action<int, int, MouseButtonKind>? MouseButtonDown;
    public event Action<int, int, MouseButtonKind>? MouseButtonUp;
    public event Action<int, int, int>? MouseWheel;

    public void Start() { }
    public void Dispose() { }

    public void RaiseMove(int x, int y) => MouseMoved?.Invoke(x, y);
    public void RaiseButtonDown(int x, int y, MouseButtonKind b) => MouseButtonDown?.Invoke(x, y, b);
    public void RaiseButtonUp(int x, int y, MouseButtonKind b) => MouseButtonUp?.Invoke(x, y, b);
    public void RaiseWheel(int x, int y, int delta) => MouseWheel?.Invoke(x, y, delta);
}

/// <summary>No-op dialogs so tests never show file windows.</summary>
public sealed class FakeDialogService : IDialogService
{
    public string? SaveFileResult { get; set; }
    public string? OpenFileResult { get; set; }

    public string? ShowSaveFileDialog(string filter, string fileName) => SaveFileResult;
    public string? ShowOpenFileDialog(string filter) => OpenFileResult;
}
