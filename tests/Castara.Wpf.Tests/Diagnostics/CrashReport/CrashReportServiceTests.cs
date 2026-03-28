using Castara.Wpf.CrashReport;
using Castara.Wpf.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using FluentAssertions;
using Moq;

namespace Castara.Wpf.Tests.Diagnostics.CrashReport;

/// <summary>
/// Contains unit tests for <see cref="CrashReportService"/>.
/// </summary>
public sealed class CrashReportServiceTests
{
    /// <summary>
    /// Verifies that <see cref="CrashReportService.HandleFatal"/> successfully orchestrates
    /// the build, write, and notification workflow when all operations complete successfully.
    /// </summary>
    [Fact]
    public void HandleFatal_ShouldBuildWriteAndNotify_WhenSuccessful()
    {
        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();

        var report = new Castara.Wpf.Diagnostics.CrashReport.CrashReport(
            ReportId: "report-123",
            Source: "DispatcherUnhandledException",
            TimestampUtc: DateTimeOffset.UtcNow,
            ApplicationName: "Castara",
            ApplicationVersion: "1.0.0.0",
            RuntimeVersion: "8.0.25",
            OperatingSystem: "Windows",
            Exception: new CrashExceptionInfo("System.InvalidOperationException", "Boom", null),
            InnerExceptions: Array.Empty<CrashExceptionInfo>(),
            Context: new Dictionary<string, string>(),
            RecentLogs: Array.Empty<CrashLogEntry>());

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        writer.Setup(x => x.Write(report))
            .Returns(@"C:\temp\report.json");

        var sut = new CrashReportService(builder.Object, writer.Object, dialog.Object);

        sut.HandleFatal(new InvalidOperationException("Boom"), "DispatcherUnhandledException");

        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Once);
        writer.Verify(x => x.Write(report), Times.Once);
        dialog.Verify(x => x.ShowCrashReportSaved(@"C:\temp\report.json", "report-123"), Times.Once);
    }

    [Fact]
    public void HandleFatal_ShouldShowFailureDialog_WhenWriterThrows()
    {
        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();

        var report = new Castara.Wpf.Diagnostics.CrashReport.CrashReport(
            ReportId: "report-123",
            Source: "DispatcherUnhandledException",
            TimestampUtc: DateTimeOffset.UtcNow,
            ApplicationName: "Castara",
            ApplicationVersion: "1.0.0.0",
            RuntimeVersion: "8.0.25",
            OperatingSystem: "Windows",
            Exception: new CrashExceptionInfo("System.InvalidOperationException", "Boom", null),
            InnerExceptions: Array.Empty<CrashExceptionInfo>(),
            Context: new Dictionary<string, string>(),
            RecentLogs: Array.Empty<CrashLogEntry>());

        builder.Setup(x => x.Build(It.IsAny<Exception>(), It.IsAny<string>()))
            .Returns(report);

        writer.Setup(x => x.Write(report))
            .Throws(new IOException("Disk full"));

        var sut = new CrashReportService(builder.Object, writer.Object, dialog.Object);

        sut.HandleFatal(new InvalidOperationException("Boom"), "DispatcherUnhandledException");

        dialog.Verify(x => x.ShowCrashReportFailed(It.IsAny<string>()), Times.Once);
    }
}