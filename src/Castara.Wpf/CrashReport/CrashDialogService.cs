using Castara.Wpf.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Upload;
using Castara.Wpf.Services.Clipboard;
using Microsoft.Extensions.Options;

namespace Castara.Wpf.CrashReport;

public sealed class CrashReportDialogService : ICrashReportDialogService
{
    private readonly IClipboardService _clipboardService;

    private readonly IOptions<CrashReportUploadOptions> _options;

    public CrashReportDialogService(IClipboardService clipboardService,
        IOptions<CrashReportUploadOptions> options)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public CrashReportDialogResult Show(string reportJson, string reportId)
    {
        var vm = new CrashReportDialogViewModel(reportJson, reportId, _options.Value.Enabled);
        var dialog = new CrashReportDialog(vm, _clipboardService);

        var accepted = dialog.ShowDialog() == true;

        return new CrashReportDialogResult(
            Accepted: accepted,
            SendReport: accepted && vm.SendReport,
            SaveLocally: accepted && vm.SaveLocally);
    }
}