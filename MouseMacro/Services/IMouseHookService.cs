using MouseMacro.Models;

namespace MouseMacro.Services;

public interface IMouseHookService : IDisposable
{
    event Action<int, int>? MouseMoved;
    event Action<int, int, MouseButtonKind>? MouseButtonDown;
    event Action<int, int, MouseButtonKind>? MouseButtonUp;
    event Action<int, int, int>? MouseWheel;

    void Start();
}
