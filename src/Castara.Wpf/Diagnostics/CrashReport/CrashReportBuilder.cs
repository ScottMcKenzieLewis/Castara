using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class CrashReportBuilder : ICrashReportBuilder
{
    private static readonly Regex WindowsPathRegex = new(
    @"(?<!\w)([A-Za-z]:\\[^\r\n\t""<>|:]*?(?:\\[^\r\n\t""<>|:]*)*)(?=:\d+|:\s*line\s+\d+|\s|$)",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UncPathRegex = new(
        @"(?<!\w)(\\\\[^\s\r\n\t""<>|]+(?:\\[^\s\r\n\t""<>|]+)*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UnixPathRegex = new(
        @"(?<!\w)(/(?:[^/\s:\r\n]+/)*[^/\s:\r\n]+)(?=:\d+|:\s*line\s+\d+|\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int MaxRecentLogEntries = 200;

    private readonly IApplicationStateSnapshotService _snapshotService;
    private readonly IObservableLogStore _logStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportBuilder"/> class.
    /// </summary>
    /// <param name="snapshotService">The service for retrieving application state snapshots.</param>
    /// <param name="logStore">The log store containing recent application logs.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="snapshotService"/> or <paramref name="logStore"/> is <see langword="null"/>.
    /// </exception>
    public CrashReportBuilder(
        IApplicationStateSnapshotService snapshotService,
        IObservableLogStore logStore)
    {
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
    }

    public CrashReport Build(Exception exception, string source)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Crash source is required.", nameof(source));

        var snapshot = _snapshotService.GetSnapshot();
        var reportId = Guid.NewGuid().ToString("N");

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";

        var context = snapshot.Values
            .ToDictionary(
                kvp => kvp.Key,
                kvp => Sanitize(kvp.Value) ?? string.Empty,
                StringComparer.Ordinal);

        var recentLogs = _logStore.Entries
            .TakeLast(MaxRecentLogEntries)
            .Select(x => new CrashLogEntry(
                TimestampUtc: x.Timestamp,
                Level: x.Level.ToString(),
                Category: Sanitize(x.Category) ?? string.Empty,
                Message: Sanitize(x.Message) ?? string.Empty))
            .ToArray();

        return new CrashReport(
            ReportId: reportId,
            TimestampUtc: DateTimeOffset.UtcNow,
            ApplicationName: "Castara",
            ApplicationVersion: version,
            RuntimeVersion: Environment.Version.ToString(),
            OperatingSystem: RuntimeInformation.OSDescription,
            Source: source.Trim(),
            Exception: ToInfo(exception),
            InnerExceptions: FlattenInnerExceptions(exception).ToArray(),
            Context: context,
            RecentLogs: recentLogs);
    }

    private static CrashExceptionInfo ToInfo(Exception ex) =>
        new(
            Type: ex.GetType().FullName ?? ex.GetType().Name,
            Message: Sanitize(ex.Message) ?? string.Empty,
            StackTrace: Sanitize(ex.StackTrace));

    private static IEnumerable<CrashExceptionInfo> FlattenInnerExceptions(Exception ex)
    {
        var current = ex.InnerException;
        while (current is not null)
        {
            yield return ToInfo(current);
            current = current.InnerException;
        }
    }

    /// <summary>
    /// Sanitizes a string by redacting file system paths while preserving filenames.
    /// This helps protect user privacy by removing personal directory paths from crash reports.
    /// </summary>
    /// <param name="value">The string value to sanitize.</param>
    /// <returns>
    /// The sanitized string with paths redacted, or the original value if it's <see langword="null"/> or whitespace.
    /// </returns>
    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = value;

        // Replace Windows paths (C:\path\to\file) with [redacted-path]\file
        sanitized = WindowsPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        // Replace UNC paths (\\server\share\file) with [redacted-unc-path]\file
        sanitized = UncPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-unc-path]"));

        // Replace Unix paths (/home/user/file) with [redacted-path]/file
        sanitized = UnixPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        return sanitized;
    }

    private static string RedactPath(string path, string token)
    {
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);

        var fileName = Path.GetFileName(normalized);

        return string.IsNullOrWhiteSpace(fileName)
            ? token
            : $"{token}{Path.DirectorySeparatorChar}{fileName}";
    }
}