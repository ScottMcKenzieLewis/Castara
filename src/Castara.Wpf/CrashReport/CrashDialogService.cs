using Castara.Wpf.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Services.Clipboard;

namespace Castara.Wpf.CrashReport;

public sealed class CrashReportDialogService : ICrashReportDialogService
{
    private readonly IClipboardService _clipboardService;

    public CrashReportDialogService(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
    }

    public CrashReportDialogResult Show(string reportJson, string reportId)
    {
        var vm = new CrashReportDialogViewModel(reportJson, reportId);
        var dialog = new CrashReportDialog(vm, _clipboardService);

        var accepted = dialog.ShowDialog() == true;

        return new CrashReportDialogResult(
            Accepted: accepted,
            SendReport: accepted && vm.SendReport,
            SaveLocally: accepted && vm.SaveLocally);
    }
}