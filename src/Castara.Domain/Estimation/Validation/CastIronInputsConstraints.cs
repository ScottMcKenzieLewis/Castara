namespace Castara.Domain.Estimation.Validation;

/// <summary>
/// Defines the valid input constraints for cast iron composition and section properties.
/// These constraints are used for validation throughout the estimation process.
/// </summary>
public static class CastIronInputConstraints
{
    #region Composition Constraints (wt%)

    /// <summary>
    /// Minimum carbon content in weight percent.
    /// </summary>
    public const double CarbonMin = 2.5;

    /// <summary>
    /// Maximum carbon content in weight percent.
    /// </summary>
    public const double CarbonMax = 4.5;

    /// <summary>
    /// Minimum silicon content in weight percent.
    /// </summary>
    public const double SiliconMin = 0.5;

    /// <summary>
    /// Maximum silicon content in weight percent.
    /// </summary>
    public const double SiliconMax = 3.5;

    /// <summary>
    /// Minimum manganese content in weight percent.
    /// </summary>
    public const double ManganeseMin = 0.0;

    /// <summary>
    /// Maximum manganese content in weight percent.
    /// </summary>
    public const double ManganeseMax = 2.0;

    /// <summary>
    /// Minimum phosphorus content in weight percent.
    /// </summary>
    public const double PhosphorusMin = 0.0;

    /// <summary>
    /// Maximum phosphorus content in weight percent.
    /// </summary>
    public const double PhosphorusMax = 1.0;

    /// <summary>
    /// Minimum sulfur content in weight percent.
    /// </summary>
    public const double SulfurMin = 0.0;

    /// <summary>
    /// Maximum sulfur content in weight percent.
    /// </summary>
    public const double SulfurMax = 1.0;

    #endregion

    #region Section Constraints

    /// <summary>
    /// Minimum section thickness in millimeters.
    /// </summary>
    public const double ThicknessMinMm = 0.0001;

    /// <summary>
    /// Minimum cooling rate in degrees Celsius per second.
    /// </summary>
    public const double CoolingRateMinCPerSec = 0.0001;

    #endregion

    #region Typical Guidance (Non-Validation)

    /// <summary>
    /// Typical minimum cooling rate in degrees Celsius per second.
    /// This is guidance only and not used for validation.
    /// </summary>
    public const double CoolingRateMin = 0.05;

    /// <summary>
    /// Maximum cooling rate in degrees Celsius per second.
    /// </summary>
    public const double CoolingRateMax = 200.0;

    #endregion
}