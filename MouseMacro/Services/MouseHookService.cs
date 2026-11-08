using System.Runtime.InteropServices;
using MouseMacro.Models;

namespace MouseMacro.Services;

/// <summary>
/// Global low-level mouse hook used to record mouse activity even when the app is not focused.
/// Must be started on a thread with a message loop (the WPF UI thread).
/// </summary>
public sealed class MouseHookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private readonly HookProc _hookProc;
    private IntPtr _hookId = IntPtr.Zero;

    public event Action<int, int>? MouseMoved;
    public event Action<int, int, MouseButtonKind>? MouseButtonDown;
    public event Action<int, int, MouseButtonKind>? MouseButtonUp;
    public event Action<int, int, int>? MouseWheel;

    public MouseHookService() => _hookProc = HookCallback;

    public void Start()
    {
        if (_hookId == IntPtr.Zero)
        {
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);
        }
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            switch ((int)wParam)
            {
                case WM_MOUSEMOVE:
                    MouseMoved?.Invoke(data.pt.X, data.pt.Y);
                    break;
                case WM_LBUTTONDOWN:
                    MouseButtonDown?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Left);
                    break;
                case WM_LBUTTONUP:
                    MouseButtonUp?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Left);
                    break;
                case WM_RBUTTONDOWN:
                    MouseButtonDown?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Right);
                    break;
                case WM_RBUTTONUP:
                    MouseButtonUp?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Right);
                    break;
                case WM_MBUTTONDOWN:
                    MouseButtonDown?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Middle);
                    break;
                case WM_MBUTTONUP:
                    MouseButtonUp?.Invoke(data.pt.X, data.pt.Y, MouseButtonKind.Middle);
                    break;
                case WM_MOUSEWHEEL:
                    MouseWheel?.Invoke(data.pt.X, data.pt.Y, (short)((data.mouseData >> 16) & 0xFFFF));
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
