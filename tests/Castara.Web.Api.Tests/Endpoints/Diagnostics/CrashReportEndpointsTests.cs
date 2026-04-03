using Castara.Api.Configuration;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Dtos.Validation;
using Castara.Web.Api.Endpoints.Diagnostics;
using Castara.Web.Api.Services.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace Castara.Web.Api.Tests.Endpoints.Diagnostics;

public sealed class CrashReportEndpointsTests
{
    [Fact]
    public async Task SubmitAsync_ShouldReturn503_WhenIngestionIsDisabled()
    {
        // Arrange
        var request = CreateRequest();

        var storageService = new Mock<ICrashReportStorageService>(MockBehavior.Strict);
        var validator = new Mock<IValidator<SubmitCrashReportRequest>>(MockBehavior.Strict);
        var validationErrorFactory = new Mock<IValidationErrorResponseFactory>(MockBehavior.Strict);

        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger("CrashReportEndpoints"))
            .Returns(logger.Object);

        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = false
        });

        var httpContext = CreateHttpContext();

        // Act
        var result = await CrashReportEndpoints.SubmitAsync(
            request,
            storageService.Object,
            validator.Object,
            validationErrorFactory.Object,
            loggerFactory.Object,
            options,
            httpContext,
            CancellationToken.None);

        // Assert
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Contains("Crash report ingestion is disabled", response.Body);

        validator.Verify(
            x => x.ValidateAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        storageService.Verify(
            x => x.StoreAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ShouldReturn400_WhenValidationFails()
    {
        // Arrange
        var request = CreateRequest();

        var validationFailures = new[]
        {
            new ValidationFailure("Report.ReportId", "ReportId is required."),
            new ValidationFailure("Report.ApplicationName", "ApplicationName is required.")
        };

        var validationResult = new ValidationResult(validationFailures);

        var storageService = new Mock<ICrashReportStorageService>(MockBehavior.Strict);

        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        validator
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validationErrorFactory = new Mock<IValidationErrorResponseFactory>();
        var errorDto = new ValidationErrorDto
        {
            TraceId = "trace-123",
            Details = new Dictionary<string, string[]>
            {
                ["Report.ReportId"] = new[] { "ReportId is required." },
                ["Report.ApplicationName"] = new[] { "ApplicationName is required." }
            }
        };

        validationErrorFactory
            .Setup(x => x.Create(validationResult, "trace-123"))
            .Returns(errorDto);

        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger("CrashReportEndpoints"))
            .Returns(logger.Object);

        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "trace-123";

        // Act
        var result = await CrashReportEndpoints.SubmitAsync(
            request,
            storageService.Object,
            validator.Object,
            validationErrorFactory.Object,
            loggerFactory.Object,
            options,
            httpContext,
            CancellationToken.None);

        // Assert
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);

        validator.Verify(
            x => x.ValidateAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);

        validationErrorFactory.Verify(
            x => x.Create(validationResult, "trace-123"),
            Times.Once);

        storageService.Verify(
            x => x.StoreAsync(It.IsAny<SubmitCrashReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ShouldReturn202_WhenRequestIsValid()
    {
        // Arrange
        var request = CreateRequest();

        var validationResult = new ValidationResult();

        var validator = new Mock<IValidator<SubmitCrashReportRequest>>();
        validator
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Replace StoreCrashReportResult with your actual return type.
        var storeResult = new StoreCrashReportResult(
            Status: "accepted",
            IncidentId: "INC-12345",
            ReceivedAtUtc: new DateTimeOffset(2026, 04, 03, 12, 00, 00, TimeSpan.Zero));

        var storageService = new Mock<ICrashReportStorageService>();
        storageService
            .Setup(x => x.StoreAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storeResult);

        var validationErrorFactory = new Mock<IValidationErrorResponseFactory>(MockBehavior.Strict);

        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger("CrashReportEndpoints"))
            .Returns(logger.Object);

        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        var httpContext = CreateHttpContext();

        // Act
        var result = await CrashReportEndpoints.SubmitAsync(
            request,
            storageService.Object,
            validator.Object,
            validationErrorFactory.Object,
            loggerFactory.Object,
            options,
            httpContext,
            CancellationToken.None);

        // Assert
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status202Accepted, response.StatusCode);

        var payload = JsonSerializer.Deserialize<SubmitCrashReportResponse>(
            response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal("INC-12345", payload!.IncidentId);
        Assert.Equal(storeResult.ReceivedAtUtc, payload.ReceivedAtUtc);
        Assert.Equal("accepted", payload.Status);

        validator.Verify(
            x => x.ValidateAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);

        storageService.Verify(
            x => x.StoreAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);

        validationErrorFactory.VerifyNoOtherCalls();
    }

    private static SubmitCrashReportRequest CreateRequest()
    {
        // Populate with the minimum valid object graph for your DTOs.
        // Replace with your actual constructors/properties.
        return new SubmitCrashReportRequest(
            Report: new CrashReportDto(
                ReportId: "report-123",
                TimestampUtc: DateTimeOffset.UtcNow,
                ApplicationName: "Castara.Wpf",
                ApplicationVersion: "1.0.0",
                RuntimeVersion: ".NET 8",
                OperatingSystem: "Windows 11",
                Source: "CrashDialog",
                Exception: new CrashExceptionInfoDto(
                    Type: "System.Exception",
                    Message: "Boom",
                    StackTrace:  "stack"
                ),
                InnerExceptions: new List<CrashExceptionInfoDto>(),
                Context: new Dictionary<string, string>(),
                RecentLogs: new List<CrashLogEntryDto>()
            )
        );
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }

    private static async Task<(int StatusCode, string Body)> ExecuteResultAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (httpContext.Response.StatusCode, body);
    }

}