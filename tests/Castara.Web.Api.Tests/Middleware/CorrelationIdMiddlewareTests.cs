using Castara.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Api.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-Id";

    [Fact]
    public async Task Invoke_ShouldReuseIncomingCorrelationId_WhenHeaderIsPresent()
    {
        // Arrange
        const string incomingCorrelationId = "01HZZZZZZZZZZZZZZZZZZZZZZZ";
        var nextCalled = false;

        RequestDelegate next = context =>
        {
            nextCalled = true;

            Assert.Equal(incomingCorrelationId, context.Items[HeaderName]);
            Assert.Equal(incomingCorrelationId, context.Response.Headers[HeaderName].ToString());

            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CorrelationIdMiddleware>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = new CorrelationIdMiddleware(next, logger.Object);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Request.Headers[HeaderName] = incomingCorrelationId;

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(incomingCorrelationId, context.Items[HeaderName]);
        Assert.Equal(incomingCorrelationId, context.Response.Headers[HeaderName].ToString());

        logger.Verify(
            x => x.BeginScope(It.IsAny<It.IsAnyType>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_ShouldGenerateCorrelationId_WhenHeaderIsMissing()
    {
        // Arrange
        var nextCalled = false;

        RequestDelegate next = context =>
        {
            nextCalled = true;

            var correlationId = Assert.IsType<string>(context.Items[HeaderName]);
            Assert.False(string.IsNullOrWhiteSpace(correlationId));
            Assert.Equal(correlationId, context.Response.Headers[HeaderName].ToString());

            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CorrelationIdMiddleware>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = new CorrelationIdMiddleware(next, logger.Object);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-456";

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.True(nextCalled);

        var correlationId = Assert.IsType<string>(context.Items[HeaderName]);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(correlationId, context.Response.Headers[HeaderName].ToString());

        logger.Verify(
            x => x.BeginScope(It.IsAny<It.IsAnyType>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_ShouldGenerateCorrelationId_WhenHeaderIsWhitespace()
    {
        // Arrange
        var nextCalled = false;

        RequestDelegate next = context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = new Mock<ILogger<CorrelationIdMiddleware>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = new CorrelationIdMiddleware(next, logger.Object);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-789";
        context.Request.Headers[HeaderName] = "   ";

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.True(nextCalled);

        var correlationId = Assert.IsType<string>(context.Items[HeaderName]);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual("   ", correlationId);
        Assert.Equal(correlationId, context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task Invoke_ShouldPreserveSameCorrelationId_InItemsAndResponse()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        var logger = new Mock<ILogger<CorrelationIdMiddleware>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = new CorrelationIdMiddleware(next, logger.Object);

        var context = new DefaultHttpContext();

        // Act
        await middleware.Invoke(context);

        // Assert
        var itemValue = Assert.IsType<string>(context.Items[HeaderName]);
        var responseValue = context.Response.Headers[HeaderName].ToString();

        Assert.Equal(itemValue, responseValue);
    }

    [Fact]
    public async Task Invoke_ShouldCallNextMiddleware_ExactlyOnce()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();

        nextMock
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<CorrelationIdMiddleware>>();
        logger
            .Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = new CorrelationIdMiddleware(nextMock.Object, logger.Object);

        var context = new DefaultHttpContext();

        // Act
        await middleware.Invoke(context);

        // Assert
        nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }
}