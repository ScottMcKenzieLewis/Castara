namespace Castara.Domain.Estimation.Models.Outputs;

/// <summary>
/// Represents a risk flag that identifies potential concerns or issues during cast iron
/// property estimation based on composition and section parameters.
/// </summary>
/// <param name="Code">
/// A unique identifier code for the risk flag (e.g., "CHILL_RISK", "SHRINK_RISK").
/// Used for programmatic filtering and categorization of risk conditions.
/// </param>
/// <param name="Name">
/// A human-readable display name for the risk flag (e.g., "Chill Risk", "Machinability Concern").
/// Suitable for presentation in user interfaces.
/// </param>
/// <param name="Severity">
/// The severity level of the risk condition, indicating the urgency and importance
/// of addressing the identified concern.
/// </param>
/// <param name="Message">
/// A detailed, context-specific description of the risk condition and its implications.
/// May include recommended actions or considerations for the casting process.
/// </param>
/// <remarks>
/// <para>
/// Risk flags are generated during the estimation process to alert users to conditions
/// that may result in:
/// <list type="bullet">
///   <item><description>Casting defects (chill, shrinkage, porosity)</description></item>
///   <item><description>Processing difficulties (machinability issues)</description></item>
///   <item><description>Out-of-specification properties (hardness, microstructure)</description></item>
/// </list>
/// </para>
/// <para>
/// Each flag combines a standardized code for programmatic handling with user-friendly
/// messages that provide actionable guidance. Flags are prioritized by severity to help
/// users focus on the most critical concerns first.
/// </para>
/// <para>
/// Common risk flag codes include:
/// <list type="table">
///   <listheader>
///     <term>Code</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term>CHILL_RISK</term>
///     <description>Risk of carbide formation in thin or fast-cooled sections</description>
///   </item>
///   <item>
///     <term>SHRINK_RISK</term>
///     <description>Risk of shrinkage porosity or inadequate feeding</description>
///   </item>
///   <item>
///     <term>MACHINABILITY</term>
///     <description>Potential machinability challenges due to hard structures</description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example of creating a risk flag:
/// <code>
/// var flag = new RiskFlag(
///     Code: "CHILL_RISK",
///     Name: "Chill Risk",
///     Severity: RiskSeverity.High,
///     Message: "High risk of chilled structure in thin/fast-cooled sections."
/// );
/// </code>
/// </example>
public sealed record RiskFlag(
    string Code,
    string Name,
    RiskSeverity Severity,
    string Message);
