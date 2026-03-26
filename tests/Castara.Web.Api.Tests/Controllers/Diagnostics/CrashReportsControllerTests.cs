using Castara.Api.Configuration;
using Castara.Api.Dtos;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Services.Diagnostics;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Castara.Api.Tests.Controllers.Diagnostics;

public sealed class CrashReportsControllerTests
{
    [Fact]
    public async Task SubmitAsync_ShouldReturn503_WhenIngestionIsDisabled()
    {
        var storage = new Mock<ICrashReportStorageService>();
        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        var errorFactory = new Mock<IValidationErrorResponseFactory>();
        var logger = new Mock<ILogger<CrashReportsController>>();

        var sut = CreateSut(
            storage,
            validator,
            errorFactory,
            logger,
            new CrashReportIngestionOptions { Enabled = false });

        var request = CreateValidRequest();

        var result = await sut.SubmitAsync(request, CancellationToken.None);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        storage.Verify(x => x.StoreAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        validator.Verify(x => x.ValidateAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ShouldReturnBadRequest_WhenValidationFails()
    {
        var storage = new Mock<ICrashReportStorageService>();
        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        var errorFactory = new Mock<IValidationErrorResponseFactory>();
        var logger = new Mock<ILogger<CrashReportsController>>();

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Report.ReportId", "ReportId is required.")
        });

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var badRequest = new BadRequestObjectResult(new
        {
            Title = "Validation failed"
        });

        errorFactory
            .Setup(x => x.Create(validationResult, It.IsAny<string>()))
            .Returns(badRequest);

        var sut = CreateSut(
            storage,
            validator,
            errorFactory,
            logger,
            new CrashReportIngestionOptions { Enabled = true });

        var request = CreateValidRequest();

        var result = await sut.SubmitAsync(request, CancellationToken.None);

        result.Result.Should().BeSameAs(badRequest);

        storage.Verify(x => x.StoreAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ShouldReturnAccepted_WhenRequestIsValid()
    {
        var storage = new Mock<ICrashReportStorageService>();
        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        var errorFactory = new Mock<IValidationErrorResponseFactory>();
        var logger = new Mock<ILogger<CrashReportsController>>();

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        storage
            .Setup(x => x.StoreAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreCrashReportResult(
                IncidentId: "cr_01HXYZ1234567890ABCDEFGHJK",
                ReceivedAtUtc: new DateTimeOffset(2026, 03, 26, 12, 00, 00, TimeSpan.Zero),
                Status: "accepted-noop"));

        var sut = CreateSut(
            storage,
            validator,
            errorFactory,
            logger,
            new CrashReportIngestionOptions { Enabled = true });

        var request = CreateValidRequest();

        var result = await sut.SubmitAsync(request, CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        var payload = accepted.Value.Should().BeOfType<SubmitCrashReportResponse>().Subject;

        payload.IncidentId.Should().Be("cr_01HXYZ1234567890ABCDEFGHJK");
        payload.ReceivedAtUtc.Should().Be(new DateTimeOffset(2026, 03, 26, 12, 00, 00, TimeSpan.Zero));
        payload.Status.Should().Be("accepted");

        storage.Verify(x => x.StoreAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ShouldPassHttpContextTraceIdentifier_ToValidationErrorResponseFactory()
    {
        var storage = new Mock<ICrashReportStorageService>();
        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        var errorFactory = new Mock<IValidationErrorResponseFactory>();
        var logger = new Mock<ILogger<CrashReportsController>>();

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Report.Source", "Source is required.")
        });

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        string? capturedTraceId = null;

        errorFactory
            .Setup(x => x.Create(validationResult, It.IsAny<string>()))
            .Callback<ValidationResult, string>((_, traceId) => capturedTraceId = traceId)
            .Returns(new BadRequestObjectResult("validation failed"));

        var sut = CreateSut(
            storage,
            validator,
            errorFactory,
            logger,
            new CrashReportIngestionOptions { Enabled = true });

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "trace-123"
            }
        };

        var request = CreateValidRequest();

        await sut.SubmitAsync(request, CancellationToken.None);

        capturedTraceId.Should().Be("trace-123");
    }

    private static CrashReportsController CreateSut(
        Mock<ICrashReportStorageService> storage,
        Mock<IValidator<SubmitCrashReportRequest>> validator,
        Mock<IValidationErrorResponseFactory> errorFactory,
        Mock<ILogger<CrashReportsController>> logger,
        CrashReportIngestionOptions options)
    {
        var controller = new CrashReportsController(
            storage.Object,
            validator.Object,
            errorFactory.Object,
            logger.Object,
            Options.Create(options));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "test-trace-id"
            }
        };

        return controller;
    }

    private static SubmitCrashReportRequest CreateValidRequest()
    {
        return new SubmitCrashReportRequest(
            Report: new CrashReportDto(
                ReportId: "abc123",
                TimestampUtc: new DateTimeOffset(2026, 03, 26, 12, 00, 00, TimeSpan.Zero),
                ApplicationName: "Castara",
                ApplicationVersion: "1.0.0.0",
                RuntimeVersion: "8.0.25",
                OperatingSystem: "Microsoft Windows 10.0.19045",
                Source: "DispatcherUnhandledException",
                Exception: new CrashExceptionInfoDto(
                    Type: "System.InvalidOperationException",
                    Message: "Intentional crash test.",
                    StackTrace: "at Castara.Wpf.ViewModels.ShellViewModel"),
                InnerExceptions: Array.Empty<CrashExceptionInfoDto>(),
                Context: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Theme"] = "Dark",
                    ["ActiveView"] = "CalculationsViewModel"
                },
                RecentLogs: Array.Empty<CrashLogEntryDto>()));
    }
}