using Castara.Api.Diagnostics.Services;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using FluentAssertions;

namespace Castara.Api.Tests.Diagnostics;

public sealed class CrashReportSanitizerTests
{
    [Fact]
    public void Sanitize_ShouldRedactWindowsPaths_AndPreserveFileName()
    {
        var sut = new CrashReportSanitizer();

        var request = new SubmitCrashReportRequest(
            Report: new CrashReportDto(
                ReportId: "r1",
                TimestampUtc: DateTimeOffset.UtcNow,
                ApplicationName: "Castara",
                ApplicationVersion: "1.0.0",
                RuntimeVersion: "8.0.25",
                OperatingSystem: "Windows",
                Source: "DispatcherUnhandledException",
                Exception: new CrashExceptionInfoDto(
                    Type: "System.InvalidOperationException",
                    Message: @"Boom at C:\Users\Scott\OneDrive\git\Castara\src\ShellViewModel.cs",
                    StackTrace: @"at Foo in C:\Users\Scott\OneDrive\git\Castara\src\ShellViewModel.cs:line 133"),
                InnerExceptions: Array.Empty<CrashExceptionInfoDto>(),
                Context: new Dictionary<string, string>
                {
                    ["RootPath"] = @"C:\Users\Scott\OneDrive\git\Castara\src\Castara.Web.Api"
                },
                RecentLogs: new[]
                {
                    new CrashLogEntryDto(
                        TimestampUtc: DateTimeOffset.UtcNow,
                        Level: "Information",
                        Category: "Test",
                        Message: @"Content root path: C:\Users\Scott\OneDrive\git\Castara\src\Castara.Web.Api\bin\Debug\net8.0")
                }));

        var result = sut.Sanitize(request);

        result.Report.Exception.Message.Should().Contain("[redacted-path]");
        result.Report.Exception.StackTrace.Should().Contain(@"[redacted-path]\ShellViewModel.cs");
        result.Report.Context["RootPath"].Should().Contain("[redacted-path]");
        result.Report.RecentLogs[0].Message.Should().Contain("[redacted-path]");
    }
}