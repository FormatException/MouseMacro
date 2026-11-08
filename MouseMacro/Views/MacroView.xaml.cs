using System.ComponentModel;
using System.Windows.Controls;
using MouseMacro.ViewModels;

namespace MouseMacro.Views;

public partial class MacroView : UserControl
{
    public MacroView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MacroViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MacroViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MacroViewModel.CurrentEventIndex)
            && sender is MacroViewModel viewModel)
        {
            HighlightEvent(viewModel.CurrentEventIndex);
        }
    }

    private void HighlightEvent(int eventIndex)
    {
        if (eventIndex < 0)
        {
            MacroTextBox.Select(0, 0);
            return;
        }

        // Skip the version header and column header lines.
        int targetLine = eventIndex + 2;
        if (targetLine >= MacroTextBox.LineCount)
        {
            return;
        }

        int charIndex = MacroTextBox.GetCharacterIndexFromLineIndex(targetLine);
        int lineLength = MacroTextBox.GetLineLength(targetLine);
        if (charIndex < 0 || lineLength <= 0)
        {
            return;
        }

        MacroTextBox.Focus();
        MacroTextBox.Select(charIndex, lineLength);
        MacroTextBox.ScrollToLine(targetLine);
    }
}
