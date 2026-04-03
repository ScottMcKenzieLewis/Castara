using System.Text.Json;
using Castara.Api.Configuration;
using Castara.Api.Middleware.Diagnostics;
using Castara.Api.Services.Diagnostics;
using Castara.Web.Api.Attributes.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Castara.Api.Tests.Middleware.Diagnostics;

public sealed class CrashReportHmacValidationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenEndpointDoesNotRequireHmac()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CrashReportHmacValidationMiddleware>>();
        var validator = new Mock<ICrashReportRequestSignatureValidator>(MockBehavior.Strict);

        var middleware = new CrashReportHmacValidationMiddleware(next, logger.Object);

        var context = CreateHttpContext(requiresHmac: false);
        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        // Act
        await middleware.InvokeAsync(context, validator.Object, options);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode == 0 ? 200 : context.Response.StatusCode);

        validator.Verify(
            x => x.IsValidAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn503_WhenEndpointRequiresHmac_AndIngestionIsDisabled()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CrashReportHmacValidationMiddleware>>();
        var validator = new Mock<ICrashReportRequestSignatureValidator>(MockBehavior.Strict);

        var middleware = new CrashReportHmacValidationMiddleware(next, logger.Object);

        var context = CreateHttpContext(requiresHmac: true);
        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = false
        });

        // Act
        await middleware.InvokeAsync(context, validator.Object, options);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        validator.Verify(
            x => x.IsValidAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var problem = await ReadProblemDetailsAsync(context);

        Assert.NotNull(problem);
        Assert.Equal("Crash report ingestion is disabled.", problem!.Title);
        Assert.Equal("The service is not currently accepting crash reports.", problem.Detail);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenEndpointRequiresHmac_AndSignatureIsInvalid()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CrashReportHmacValidationMiddleware>>();
        var validator = new Mock<ICrashReportRequestSignatureValidator>();

        validator
            .Setup(x => x.IsValidAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var middleware = new CrashReportHmacValidationMiddleware(next, logger.Object);

        var context = CreateHttpContext(requiresHmac: true);
        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        // Act
        await middleware.InvokeAsync(context, validator.Object, options);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        validator.Verify(
            x => x.IsValidAsync(context.Request, context.RequestAborted),
            Times.Once);

        var problem = await ReadProblemDetailsAsync(context);

        Assert.NotNull(problem);
        Assert.Equal("Unauthorized", problem!.Title);
        Assert.Equal("A valid request signature is required.", problem.Detail);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);

        VerifyLog(
            logger,
            LogLevel.Warning,
            "Crash report rejected due to invalid HMAC signature.");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenEndpointRequiresHmac_AndSignatureIsValid()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CrashReportHmacValidationMiddleware>>();
        var validator = new Mock<ICrashReportRequestSignatureValidator>();

        validator
            .Setup(x => x.IsValidAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var middleware = new CrashReportHmacValidationMiddleware(next, logger.Object);

        var context = CreateHttpContext(requiresHmac: true);
        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        // Act
        await middleware.InvokeAsync(context, validator.Object, options);

        // Assert
        Assert.True(nextCalled);

        validator.Verify(
            x => x.IsValidAsync(context.Request, context.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrow_WhenContextIsNull()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<CrashReportHmacValidationMiddleware>>();
        var validator = new Mock<ICrashReportRequestSignatureValidator>();
        var middleware = new CrashReportHmacValidationMiddleware(next, logger.Object);

        var options = Options.Create(new CrashReportIngestionOptions
        {
            Enabled = true
        });

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.InvokeAsync(null!, validator.Object, options));
    }

    private static DefaultHttpContext CreateHttpContext(bool requiresHmac)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        Endpoint endpoint;

        if (requiresHmac)
        {
            endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new RequireCrashReportHmacAttribute()),
                "CrashReportEndpoint");
        }
        else
        {
            endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(),
                "NonCrashReportEndpoint");
        }

        context.SetEndpoint(endpoint);

        return context;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<ProblemDetails>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static void VerifyLog(
        Mock<ILogger<CrashReportHmacValidationMiddleware>> logger,
        LogLevel level,
        string expectedMessage)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}