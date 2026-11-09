namespace MouseMacro.Services;

public interface IDialogService
{
    string? ShowSaveFileDialog(string filter, string fileName);
    string? ShowOpenFileDialog(string filter);
}
