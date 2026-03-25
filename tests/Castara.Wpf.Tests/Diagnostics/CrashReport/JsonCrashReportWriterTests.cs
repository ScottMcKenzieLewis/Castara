using System.Text.Json;
using Castara.Wpf.Diagnostics.CrashReport;
using FluentAssertions;

namespace Castara.Wpf.Tests.Diagnostics.CrashReport;

/// <summary>
/// Contains unit tests for <see cref="JsonCrashReportWriter"/>.
/// </summary>
public sealed class JsonCrashReportWriterTests
{
    /// <summary>
    /// Verifies that <see cref="JsonCrashReportWriter.Write"/> correctly serializes a crash report to JSON,
    /// persists it to a file, and that the file can be deserialized back to the original report (round-trip test).
    /// </summary>
    [Fact]
    public void Write_ShouldPersistJsonFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Castara.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sut = new JsonCrashReportWriter();

            var report = new Castara.Wpf.Diagnostics.CrashReport.CrashReport(
                ReportId: "abc123",
                Source: "DispatcherUnhandledException",
                TimestampUtc: DateTimeOffset.UtcNow,
                ApplicationName: "Castara",
                ApplicationVersion: "1.0.0.0",
                RuntimeVersion: "8.0.25",
                OperatingSystem: "Windows",
                Exception: new CrashExceptionInfo("System.InvalidOperationException", "Boom", null),
                InnerExceptions: Array.Empty<CrashExceptionInfo>(),
                Context: new Dictionary<string, string> { ["Theme"] = "Light" },
                RecentLogs: Array.Empty<CrashLogEntry>());

            var path = sut.Write(report);

            File.Exists(path).Should().BeTrue();

            var json = File.ReadAllText(path);
            var roundTrip = JsonSerializer.Deserialize<Castara.Wpf.Diagnostics.CrashReport.CrashReport>(json);

            roundTrip.Should().NotBeNull();
            roundTrip!.ReportId.Should().Be("abc123");
            roundTrip.Source.Should().Be("DispatcherUnhandledException");
            roundTrip.Context["Theme"].Should().Be("Light");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}