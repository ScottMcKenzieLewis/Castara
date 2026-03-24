using Castara.Wpf.Models;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel;

namespace Castara.Wpf.Services.Status;

/// <summary>
/// Provides a centralized service for managing and broadcasting application status changes.
/// </summary>
public sealed class StatusService : IStatusService
{
    private readonly ILogger<StatusService> _logger;

    private StatusState _current = new(AppStatusLevel.Ok, "Ready", "Ready for Calculation");

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging status changes.</param>
    public StatusService(ILogger<StatusService> logger)
    {
        _logger = logger ?? NullLogger<StatusService>.Instance;
    }

    /// <summary>
    /// Gets the current application status state.
    /// </summary>
    public StatusState Current
    {
        get => _current;
        private set
        {
            if (Equals(_current, value)) return;
            _current = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        }
    }

    /// <summary>
    /// Occurs when the <see cref="Current"/> property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the application status to the specified state.
    /// </summary>
    /// <param name="state">The new status state.</param>
    public void Set(StatusState state)
    {
        LogStatus(state);
        Current = state;
    }

    /// <summary>
    /// Sets the application status using the specified level and text values.
    /// </summary>
    /// <param name="level">The status level (Ok, Warning, or Error).</param>
    /// <param name="leftText">The text to display on the left side of the status bar.</param>
    /// <param name="rightText">The text to display on the right side of the status bar.</param>
    public void Set(AppStatusLevel level, string leftText, string rightText)
    {
        var state = new StatusState(level, leftText, rightText);
        LogStatus(state);
        Current = state;
    }

    /// <summary>
    /// Logs the status change at the appropriate log level based on the status level.
    /// </summary>
    /// <param name="state">The status state to log.</param>
    private void LogStatus(StatusState state)
    {
        var message = "App status changed. Level={Level}, LeftText={LeftText}, RightText={RightText}";

        switch (state.Level)
        {
            case AppStatusLevel.Error:
                _logger.LogError(message, state.Level, state.LeftText, state.RightText);
                break;

            case AppStatusLevel.Warning:
                _logger.LogWarning(message, state.Level, state.LeftText, state.RightText);
                break;

            default:
                _logger.LogInformation(message, state.Level, state.LeftText, state.RightText);
                break;
        }
    }
}