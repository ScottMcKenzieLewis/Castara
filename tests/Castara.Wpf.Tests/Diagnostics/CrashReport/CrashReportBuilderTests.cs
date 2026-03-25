using Castara.Wpf.Diagnostics;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.ObjectModel;

namespace Castara.Wpf.Tests.Diagnostics.CrashReport;

/// <summary>
/// Contains unit tests for <see cref="CrashReportBuilder"/>.
/// </summary>
public sealed class CrashReportBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="CrashReportBuilder.Build"/> populates all core crash report metadata
    /// including source, application name, exception details, and application state context.
    /// </summary>
    [Fact]
    public void Build_ShouldPopulateCoreMetadata()
    {
        var snapshot = new Mock<IApplicationStateSnapshotService>();
        snapshot.Setup(x => x.GetSnapshot()).Returns(
            new ApplicationStateSnapshot(new Dictionary<string, string>
            {
                [ApplicationStateKeys.Theme] = "Light",
                [ApplicationStateKeys.ActiveView] = "CalculationsViewModel",
                [ApplicationStateKeys.CastingProfile] = "Green Sand - Gray Iron - General",
                [ApplicationStateKeys.UnitSystem] = "Standard",
                [ApplicationStateKeys.Carbon] = "3.4"
            }));

        var store = new Mock<IObservableLogStore>();
        store.SetupGet(x => x.Entries).Returns(
            new ReadOnlyObservableCollection<LogEntry>(
                new ObservableCollection<LogEntry>()));

        var sut = new CrashReportBuilder(snapshot.Object, store.Object);

        var ex = new InvalidOperationException("Intentional crash test.");

        var report = sut.Build(ex, "DispatcherUnhandledException");

        report.Source.Should().Be("DispatcherUnhandledException");
        report.ApplicationName.Should().Be("Castara");
        report.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
        report.Exception.Message.Should().Be("Intentional crash test.");
        report.Context[ApplicationStateKeys.Theme].Should().Be("Light");
        report.Context[ApplicationStateKeys.ActiveView].Should().Be("CalculationsViewModel");
        report.Context[ApplicationStateKeys.Carbon].Should().Be("3.4");
    }

    /// <summary>
    /// Verifies that <see cref="CrashReportBuilder.Build"/> flattens all nested inner exceptions
    /// into a collection for comprehensive error diagnostics.
    /// </summary>
    [Fact]
    public void Build_ShouldFlattenInnerExceptions()
    {
        var snapshot = new Mock<IApplicationStateSnapshotService>();
        snapshot.Setup(x => x.GetSnapshot()).Returns(
            new ApplicationStateSnapshot(new Dictionary<string, string>()));

        var store = new Mock<IObservableLogStore>();
        store.SetupGet(x => x.Entries).Returns(
            new ReadOnlyObservableCollection<LogEntry>(
                new ObservableCollection<LogEntry>()));

        var sut = new CrashReportBuilder(snapshot.Object, store.Object);

        var ex = new InvalidOperationException(
            "Outer",
            new ApplicationException("Middle", new Exception("Inner")));

        var report = sut.Build(ex, "DispatcherUnhandledException");

        report.InnerExceptions.Should().HaveCount(2);
        report.InnerExceptions[0].Message.Should().Be("Middle");
        report.InnerExceptions[1].Message.Should().Be("Inner");
    }

    /// <summary>
    /// Verifies that <see cref="CrashReportBuilder.Build"/> sanitizes file paths and usernames
    /// in exception messages, application state, and log entries to protect user privacy,
    /// while preserving filenames for debugging purposes.
    /// </summary>
    [Fact]
    public void Build_ShouldSanitizePathsAndUserName_InExceptionAndLogs()
    {
        var snapshot = new Mock<IApplicationStateSnapshotService>();
        snapshot.Setup(x => x.GetSnapshot()).Returns(
            new ApplicationStateSnapshot(new Dictionary<string, string>
            {
                ["SomeKey"] = $@"C:\Users\{Environment.UserName}\secret\file.txt"
            }));

        var entries = new ObservableCollection<LogEntry>
        {
            new(
                DateTimeOffset.Now,
                LogLevel.Information,
                "Test.Category",
                new EventId(1),
                $@"Root path: C:\Users\{Environment.UserName}\OneDrive\git\Castara\src\App.xaml.cs",
                null,
                Array.Empty<KeyValuePair<string, object?>>(),
                Array.Empty<KeyValuePair<string, object?>>())
        };

        var store = new Mock<IObservableLogStore>();
        store.SetupGet(x => x.Entries).Returns(
            new ReadOnlyObservableCollection<LogEntry>(entries));

        var sut = new CrashReportBuilder(snapshot.Object, store.Object);

        var ex = new InvalidOperationException(
            $@"Boom at C:\Users\{Environment.UserName}\OneDrive\git\Castara\src\ShellViewModel.cs");

        var report = sut.Build(ex, "DispatcherUnhandledException");

        report.Exception.Message.Should().NotContain(Environment.UserName);
        report.Exception.Message.Should().Contain("[redacted-path]");
        report.Context["SomeKey"].Should().NotContain(Environment.UserName);
        report.Context["SomeKey"].Should().Contain("[redacted-path]");
        report.RecentLogs[0].Message.Should().Contain("[redacted-path]");
        report.RecentLogs[0].Message.Should().NotContain(Environment.UserName);
    }

    /// <summary>
    /// Verifies that <see cref="CrashReportBuilder.Build"/> limits recent log entries to 200,
    /// taking the most recent entries when more are available.
    /// </summary>
    [Fact]
    public void Build_ShouldCapRecentLogsTo200()
    {
        var snapshot = new Mock<IApplicationStateSnapshotService>();
        snapshot.Setup(x => x.GetSnapshot()).Returns(
            new ApplicationStateSnapshot(new Dictionary<string, string>()));

        var entries = new ObservableCollection<LogEntry>();

        for (var i = 0; i < 250; i++)
        {
            entries.Add(new LogEntry(
                DateTimeOffset.Now.AddSeconds(i),
                LogLevel.Information,
                "Test.Category",
                new EventId(i),
                $"Message {i}",
                null,
                Array.Empty<KeyValuePair<string, object?>>(),
                Array.Empty<KeyValuePair<string, object?>>()));
        }

        var store = new Mock<IObservableLogStore>();
        store.SetupGet(x => x.Entries).Returns(
            new ReadOnlyObservableCollection<LogEntry>(entries));

        var sut = new CrashReportBuilder(snapshot.Object, store.Object);

        var report = sut.Build(new InvalidOperationException("Boom"), "DispatcherUnhandledException");

        report.RecentLogs.Should().HaveCount(200);
        report.RecentLogs.Last().Message.Should().Be("Message 249");
    }
}