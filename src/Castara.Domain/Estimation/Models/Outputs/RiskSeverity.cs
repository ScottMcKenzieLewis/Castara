namespace Castara.Domain.Estimation.Models.Outputs;

/// <summary>
/// Defines the severity levels for risk flags identified during cast iron property estimation.
/// </summary>
/// <remarks>
/// Risk severity is determined by calculating a normalized risk score (0-1) and mapping it
/// to these levels. Severity levels help prioritize issues requiring attention in casting
/// operations, with higher severities indicating conditions that warrant immediate review.
/// </remarks>
public enum RiskSeverity
{
    /// <summary>
    /// Indicates a low-severity risk condition that is within acceptable parameters.
    /// </summary>
    /// <remarks>
    /// Low severity flags represent normal operating conditions or minimal concerns
    /// that do not require special attention. These conditions are expected to produce
    /// acceptable casting results under standard practices.
    /// </remarks>
    Low = 1,

    /// <summary>
    /// Indicates a medium-severity risk condition that warrants monitoring or verification.
    /// </summary>
    /// <remarks>
    /// Medium severity flags suggest conditions that are approaching concerning levels
    /// but may still be acceptable with proper process control. These conditions should
    /// be reviewed to ensure gating, risering, or other process parameters are adequate.
    /// </remarks>
    Medium = 2,

    /// <summary>
    /// Indicates a high-severity risk condition requiring immediate attention and review.
    /// </summary>
    /// <remarks>
    /// High severity flags represent conditions with significant risk of casting defects,
    /// poor machinability, or other quality issues. These conditions typically require
    /// process modifications, composition adjustments, or additional quality controls
    /// to achieve acceptable results.
    /// </remarks>
    High = 3
}