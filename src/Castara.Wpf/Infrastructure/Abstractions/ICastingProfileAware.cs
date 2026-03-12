using Castara.Domain.Casting;

namespace Castara.Wpf.Infrastructure.Abstractions;

/// <summary>
/// Defines a contract for components that need to respond to casting profile selection changes.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables view models and services to be notified when the user selects
/// a different casting profile, allowing them to update validation constraints, default values,
/// and estimation parameters accordingly.
/// </para>
/// <para>
/// <strong>Typical Implementers:</strong>
/// <list type="bullet">
///   <item><description>Calculations view model - updates composition ranges and section defaults</description></item>
///   <item><description>Validation services - adjusts constraint checking based on profile</description></item>
///   <item><description>Estimation services - applies profile-specific tuning parameters</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Profile Selection Flow:</strong>
/// <list type="number">
///   <item><description>User selects a profile from UI (e.g., "Green Sand Gray Iron - Class 30")</description></item>
///   <item><description>Shell view model calls <see cref="SetCastingProfile"/> on all registered implementers</description></item>
///   <item><description>Implementers update their internal state and constraints based on the profile</description></item>
///   <item><description>UI reflects new validation ranges, defaults, and available options</description></item>
/// </list>
/// </para>
/// <para>
/// This pattern allows centralized profile management in the shell while enabling individual
/// components to react to profile changes independently.
/// </para>
/// </remarks>
public interface ICastingProfileAware
{
    /// <summary>
    /// Updates the component's state and constraints based on the selected casting profile.
    /// </summary>
    /// <param name="profile">
    /// The casting profile definition containing composition constraints, process parameters,
    /// and risk assessment thresholds. Provides access to composition ranges, default values,
    /// and process-specific tuning factors.
    /// </param>
    /// <remarks>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    ///   <item><description>Update validation ranges for composition inputs based on the profile's min/max constraints</description></item>
    ///   <item><description>Apply profile-specific default values (e.g., <see cref="CastingProfileDefinition.DefaultSectionThicknessMm"/>)</description></item>
    ///   <item><description>Adjust UI labels and tooltips to reflect profile constraints and process family</description></item>
    ///   <item><description>Reset any existing calculations or results if profile-dependent</description></item>
    ///   <item><description>Raise property change notifications for affected properties</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example Implementation:</strong>
    /// <code>
    /// public void SetCastingProfile(CastingProfileDefinition profile)
    /// {
    ///     _currentProfile = profile;
    ///     
    ///     // Rebuild validators with profile-specific ranges
    ///     _carbonField = NumericTextField.Range(
    ///         "Carbon", 
    ///         profile.CarbonMin, 
    ///         profile.CarbonMax);
    ///     
    ///     // Apply profile defaults
    ///     ThicknessValue = profile.DefaultSectionThicknessMm;
    ///     
    ///     // Update tooltips with profile-specific ranges
    ///     OnPropertyChanged(nameof(CarbonTooltip));
    ///     OnPropertyChanged(nameof(ThicknessTooltip));
    ///     
    ///     // Re-seed text fields from canonical values
    ///     SeedAllTextFromNumerics();
    ///     
    ///     // Clear any existing results as they may not be valid for new profile
    ///     Result = null;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Profile Properties:</strong> The <paramref name="profile"/> parameter provides access to:
    /// <list type="bullet">
    ///   <item><description>Composition ranges (<c>CarbonMin/Max</c>, <c>SiliconMin/Max</c>, etc.)</description></item>
    ///   <item><description>Default values (<c>DefaultSectionThicknessMm</c>)</description></item>
    ///   <item><description>Carbon equivalent preferences (<c>PreferredCarbonEquivalentMin/Max</c>)</description></item>
    ///   <item><description>Risk thresholds (<c>ChillRiskCeiling</c>, <c>ShrinkageRiskFloor</c>, <c>HardnessWarningMin/MaxBhn</c>)</description></item>
    ///   <item><description>Process tuning factors (<c>GraphitizationBias</c>, <c>CoolingSeverityFactor</c>)</description></item>
    ///   <item><description>Metadata (<c>Id</c>, <c>DisplayName</c>, <c>ProcessFamily</c>, <c>IronType</c>)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    void SetCastingProfile(CastingProfileDefinition profile);
}
