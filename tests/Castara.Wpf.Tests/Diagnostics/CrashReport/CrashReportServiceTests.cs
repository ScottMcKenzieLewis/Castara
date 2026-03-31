using Castara.Wpf.CrashReport;
using Castara.Wpf.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport.Upload.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Castara.Wpf.Tests.Diagnostics.CrashReport;

/// <summary>
/// Contains unit tests for <see cref="CrashReportService"/>.
/// </summary>
public sealed class CrashReportServiceTests
{
    [Fact]
    public async Task HandleFatalAsync_ShouldThrow_WhenExceptionIsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleFatalAsync(
            null!,
            "DispatcherUnhandledException");

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task HandleFatalAsync_ShouldThrow_WhenSourceIsNullOrWhitespace(string? source)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            source!);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("source")
            .WithMessage("Crash source is required.*");
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldBuildAndShowDialogOnly_WhenUserDismisses()
    {
        // Arrange
        var report = CreateReport();

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>(MockBehavior.Strict);
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Returns(new CrashReportDialogResult(
                Accepted: false,
                SendReport: false,
                SaveLocally: false));

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Once);
        dialog.Verify(x => x.Show(It.IsAny<string>(), report.ReportId), Times.Once);
        writer.Verify(x => x.Write(It.IsAny<Castara.Wpf.Diagnostics.CrashReport.CrashReport>()), Times.Never);
        uploader.Verify(
            x => x.UploadAsync(It.IsAny<Castara.Wpf.Diagnostics.CrashReport.CrashReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldWriteLocally_WhenUserChoosesSaveOnly()
    {
        // Arrange
        var report = CreateReport();

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Returns(new CrashReportDialogResult(
                Accepted: true,
                SendReport: false,
                SaveLocally: true));

        writer.Setup(x => x.Write(report))
            .Returns(@"C:\temp\report.json");

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Once);
        dialog.Verify(x => x.Show(It.IsAny<string>(), report.ReportId), Times.Once);
        writer.Verify(x => x.Write(report), Times.Once);
        uploader.Verify(
            x => x.UploadAsync(It.IsAny<Castara.Wpf.Diagnostics.CrashReport.CrashReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldUpload_WhenUserChoosesSendOnly()
    {
        // Arrange
        var report = CreateReport();

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>(MockBehavior.Strict);
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>();
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Returns(new CrashReportDialogResult(
                Accepted: true,
                SendReport: true,
                SaveLocally: false));

        uploader.Setup(x => x.UploadAsync(report, CancellationToken.None))
            .ReturnsAsync(new CrashReportUploadResult(
                Success: true,
                IncidentId: "inc-123",
                Status: "OK",
                ErrorMessage: null));

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Once);
        dialog.Verify(x => x.Show(It.IsAny<string>(), report.ReportId), Times.Once);
        writer.Verify(x => x.Write(It.IsAny<Castara.Wpf.Diagnostics.CrashReport.CrashReport>()), Times.Never);
        uploader.Verify(x => x.UploadAsync(report, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldWriteAndUpload_WhenUserChoosesBoth()
    {
        // Arrange
        var report = CreateReport();

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>();
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Returns(new CrashReportDialogResult(
                Accepted: true,
                SendReport: true,
                SaveLocally: true));

        writer.Setup(x => x.Write(report))
            .Returns(@"C:\temp\report.json");

        uploader.Setup(x => x.UploadAsync(report, CancellationToken.None))
            .ReturnsAsync(new CrashReportUploadResult(
                Success: true,
                IncidentId: "inc-123",
                Status: "OK",
                ErrorMessage: null));

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        writer.Verify(x => x.Write(report), Times.Once);
        uploader.Verify(x => x.UploadAsync(report, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldNotThrow_WhenUploadFails()
    {
        // Arrange
        var report = CreateReport();

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>(MockBehavior.Strict);
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>();
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.Setup(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Returns(new CrashReportDialogResult(
                Accepted: true,
                SendReport: true,
                SaveLocally: false));

        uploader.Setup(x => x.UploadAsync(report, CancellationToken.None))
            .ReturnsAsync(new CrashReportUploadResult(
                Success: false,
                IncidentId: null,
                Status: "InternalServerError",
                ErrorMessage: "Server unavailable"));

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        var act = async () => await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        await act.Should().NotThrowAsync();
        uploader.Verify(x => x.UploadAsync(report, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldFallbackToLocalSave_WhenPrimaryWorkflowThrows()
    {
        // Arrange
        var report = CreateReport();
        var fallbackReport = CreateReport(reportId: "fallback-123");

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.SetupSequence(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report)
            .Returns(fallbackReport);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Throws(new InvalidOperationException("Dialog failed"));

        writer.Setup(x => x.Write(fallbackReport))
            .Returns(@"C:\temp\fallback-report.json");

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        var act = async () => await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        await act.Should().NotThrowAsync();
        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Exactly(2));
        dialog.Verify(x => x.Show(It.IsAny<string>(), report.ReportId), Times.Once);
        writer.Verify(x => x.Write(fallbackReport), Times.Once);
        uploader.Verify(
            x => x.UploadAsync(It.IsAny<Castara.Wpf.Diagnostics.CrashReport.CrashReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleFatalAsync_ShouldSwallowException_WhenFallbackAlsoFails()
    {
        // Arrange
        var report = CreateReport();
        var fallbackReport = CreateReport(reportId: "fallback-123");

        var builder = new Mock<ICrashReportBuilder>();
        var writer = new Mock<ICrashReportWriter>();
        var dialog = new Mock<ICrashReportDialogService>();
        var uploader = new Mock<ICrashReportUploader>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CrashReportService>>();

        builder.SetupSequence(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"))
            .Returns(report)
            .Returns(fallbackReport);

        dialog.Setup(x => x.Show(It.IsAny<string>(), report.ReportId))
            .Throws(new InvalidOperationException("Dialog failed"));

        writer.Setup(x => x.Write(fallbackReport))
            .Throws(new IOException("Disk full"));

        var sut = CreateSut(builder, writer, dialog, uploader, logger);

        // Act
        var act = async () => await sut.HandleFatalAsync(
            new InvalidOperationException("Boom"),
            "DispatcherUnhandledException");

        // Assert
        await act.Should().NotThrowAsync();
        builder.Verify(x => x.Build(It.IsAny<Exception>(), "DispatcherUnhandledException"), Times.Exactly(2));
        writer.Verify(x => x.Write(fallbackReport), Times.Once);
    }

    private static CrashReportService CreateSut(
        Mock<ICrashReportBuilder>? builder = null,
        Mock<ICrashReportWriter>? writer = null,
        Mock<ICrashReportDialogService>? dialog = null,
        Mock<ICrashReportUploader>? uploader = null,
        Mock<ILogger<CrashReportService>>? logger = null)
    {
        return new CrashReportService(
            (builder ?? new Mock<ICrashReportBuilder>()).Object,
            (writer ?? new Mock<ICrashReportWriter>()).Object,
            (dialog ?? new Mock<ICrashReportDialogService>()).Object,
            (uploader ?? new Mock<ICrashReportUploader>()).Object,
            (logger ?? new Mock<ILogger<CrashReportService>>()).Object);
    }

    private static Castara.Wpf.Diagnostics.CrashReport.CrashReport CreateReport(string reportId = "report-123")
    {
        return new Castara.Wpf.Diagnostics.CrashReport.CrashReport(
            ReportId: reportId,
            Source: "DispatcherUnhandledException",
            TimestampUtc: DateTimeOffset.UtcNow,
            ApplicationName: "Castara",
            ApplicationVersion: "1.0.0.0",
            RuntimeVersion: "8.0.25",
            OperatingSystem: "Windows",
            Exception: new CrashExceptionInfo(
                Type: "System.InvalidOperationException",
                Message: "Boom",
                StackTrace: null),
            InnerExceptions: Array.Empty<CrashExceptionInfo>(),
            Context: new Dictionary<string, string>(),
            RecentLogs: Array.Empty<CrashLogEntry>());
    }
}