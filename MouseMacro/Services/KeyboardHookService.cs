using System.Runtime.InteropServices;

namespace MouseMacro.Services;

/// <summary>
/// Global low-level keyboard hook that raises events for the
/// Left Shift + Ctrl + S (toggle start/stop) and
/// Left Shift + Ctrl + R (toggle macro recording) hotkeys.
/// Must be started on a thread with a message loop (the WPF UI thread).
/// </summary>
public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_S = 0x53;
    private const int VK_R = 0x52;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private readonly HookProc _hookProc;
    private IntPtr _hookId = IntPtr.Zero;

    public event Action? StartStopRequested;
    public event Action? RecordToggleRequested;

    public KeyboardHookService() => _hookProc = HookCallback;

    public void Start()
    {
        if (_hookId == IntPtr.Zero)
        {
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(null), 0);
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
            int message = (int)wParam;
            if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if ((vkCode == VK_S || vkCode == VK_R)
                    && IsKeyDown(VK_LSHIFT)
                    && (IsKeyDown(VK_LCONTROL) || IsKeyDown(VK_RCONTROL)))
                {
                    if (vkCode == VK_S)
                    {
                        StartStopRequested?.Invoke();
                    }
                    else
                    {
                        RecordToggleRequested?.Invoke();
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool IsKeyDown(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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
