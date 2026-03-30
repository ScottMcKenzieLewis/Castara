using System.Windows;
using System.Windows.Input;
using Castara.Wpf.Services.Clipboard;

namespace Castara.Wpf.Diagnostics.CrashReport;

public partial class CrashReportDialog : Window
{
    public CrashReportDialog(
        CrashReportDialogViewModel viewModel,
        IClipboardService clipboardService)
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboardService);

        DataContext = viewModel;

        viewModel.CloseRequested += (_, accepted) =>
        {
            DialogResult = accepted;
            Close();
        };

        viewModel.CopyRequested += (_, text) =>
        {
            clipboardService.SetText(text);
        };
    }
}