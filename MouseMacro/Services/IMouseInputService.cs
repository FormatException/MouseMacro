using MouseMacro.Models;

namespace MouseMacro.Services;

public interface IMouseInputService
{
    void MoveTo(int x, int y);
    void ButtonDownAt(int x, int y, MouseButtonKind button);
    void ButtonUpAt(int x, int y, MouseButtonKind button);
    void Wheel(int delta);
    void Click(MouseButtonKind button);
}
