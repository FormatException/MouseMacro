using System.Runtime.InteropServices;
using MouseMacro.Models;

namespace MouseMacro.Services;

/// <summary>
/// Sends synthetic mouse input (movement, button presses, wheel) via SendInput.
/// </summary>
public sealed class MouseInputService : IMouseInputService
{
    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    public void MoveTo(int x, int y) =>
        Send(NormalizeX(x), NormalizeY(y), 0, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK);

    public void ButtonDownAt(int x, int y, MouseButtonKind button) =>
        Send(NormalizeX(x), NormalizeY(y), 0, DownFlag(button) | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK);

    public void ButtonUpAt(int x, int y, MouseButtonKind button) =>
        Send(NormalizeX(x), NormalizeY(y), 0, UpFlag(button) | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK);

    public void Wheel(int delta) => Send(0, 0, unchecked((uint)delta), MOUSEEVENTF_WHEEL);

    public void Click(MouseButtonKind button)
    {
        Send(0, 0, 0, DownFlag(button));
        Send(0, 0, 0, UpFlag(button));
    }

    private static uint DownFlag(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Right => MOUSEEVENTF_RIGHTDOWN,
        MouseButtonKind.Middle => MOUSEEVENTF_MIDDLEDOWN,
        _ => MOUSEEVENTF_LEFTDOWN
    };

    private static uint UpFlag(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Right => MOUSEEVENTF_RIGHTUP,
        MouseButtonKind.Middle => MOUSEEVENTF_MIDDLEUP,
        _ => MOUSEEVENTF_LEFTUP
    };

    private static int NormalizeX(int x)
    {
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        return (int)Math.Round((x - left) * 65535.0 / Math.Max(1, width - 1));
    }

    private static int NormalizeY(int y)
    {
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        return (int)Math.Round((y - top) * 65535.0 / Math.Max(1, height - 1));
    }

    private static void Send(int dx, int dy, uint mouseData, uint flags)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
