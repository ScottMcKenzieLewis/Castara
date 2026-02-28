using System;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf.Telemetry.Logging;

/// <summary>
/// Logger provider that creates <see cref="LogStoreLogger"/> instances writing to an <see cref="ILogStore"/>.
/// </summary>
/// <remarks>
/// This provider integrates the log store with the standard Microsoft.Extensions.Logging infrastructure,
/// allowing logs to be captured and displayed in the WPF application's diagnostic UI.
/// Supports external scopes for correlation and structured logging.
/// </remarks>
public sealed class LogStoreLoggingProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ILogStore _store;
    private IExternalScopeProvider? _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogStoreLoggingProvider"/> class.
    /// </summary>
    /// <param name="store">The log store where all created loggers will write entries.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public LogStoreLoggingProvider(ILogStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance for the specified category.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger, typically the type name.</param>
    /// <returns>A new logger instance that writes to the configured log store.</returns>
    public ILogger CreateLogger(string categoryName)
        => new LogStoreLogger(categoryName, _store, () => _scopeProvider);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    /// <remarks>
    /// This provider has no unmanaged resources to dispose.
    /// </remarks>
    public void Dispose()
    {
        // No unmanaged resources
    }

    /// <summary>
    /// Sets the external scope provider for loggers created by this provider.
    /// </summary>
    /// <param name="scopeProvider">The scope provider to use for capturing logging scopes.</param>
    /// <remarks>
    /// This method is called by the logging infrastructure to enable scope support.
    /// Scopes are used to group related log entries for correlation and context.
    /// </remarks>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;
}