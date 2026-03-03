using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf.Infrastructure.Telemetry.Logging;

/// <summary>
/// Represents a structured log entry containing timestamp, level, category, message, and associated metadata.
/// </summary>
/// <param name="Timestamp">The date and time when the log entry was created.</param>
/// <param name="Level">The severity level of the log entry.</param>
/// <param name="Category">The category or logger name that produced this entry.</param>
/// <param name="EventId">The event identifier associated with the log entry.</param>
/// <param name="Message">The formatted log message.</param>
/// <param name="Exception">The exception associated with the log entry, if any.</param>
/// <param name="Properties">The structured properties attached to this log entry.</param>
/// <param name="Scopes">The active logging scopes at the time this entry was created.</param>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    EventId EventId,
    string Message,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Properties,
    IReadOnlyList<KeyValuePair<string, object?>> Scopes);