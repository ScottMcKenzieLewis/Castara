using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Castara.Wpf.Tests.TestHelpers;

/// <summary>
/// Provides extension methods for verifying <see cref="ILogger{TCategoryName}"/> mock calls in unit tests.
/// </summary>
internal static class LoggerMoqExtensions
{
    /// <summary>
    /// Verifies that a logger was called with a specific log level and a message containing the specified fragment.
    /// </summary>
    /// <typeparam name="T">The type used for the logger category.</typeparam>
    /// <param name="logger">The mocked logger to verify.</param>
    /// <param name="level">The expected log level.</param>
    /// <param name="containsMessageFragment">A fragment that should appear in the log message (case-insensitive).</param>
    /// <param name="times">The expected number of times the log method should have been called.</param>
    /// <remarks>
    /// This method performs a case-insensitive search for the message fragment in the formatted log message.
    /// It's useful for verifying that specific log statements were executed during test scenarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Verify that an error was logged exactly once with a message containing "validation failed"
    /// mockLogger.VerifyLog(LogLevel.Error, "validation failed", Times.Once());
    /// </code>
    /// </example>
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        string containsMessageFragment,
        Times times)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString() != null && v.ToString()!.Contains(containsMessageFragment, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}