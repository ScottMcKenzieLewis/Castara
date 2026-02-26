using System;
using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Domain.Estimation.Validation;

/// <summary>
/// Provides validation guards for <see cref="SectionProfile"/> instances to ensure
/// physical parameters are within valid and realistic ranges for cast iron analysis.
/// </summary>
public static class SectionGuards
{
    /// <summary>
    /// Validates a <see cref="SectionProfile"/> instance, ensuring all physical parameters
    /// are finite, positive, and within realistic ranges for cast iron casting operations.
    /// </summary>
    /// <param name="section">The section profile to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="section"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any of the following conditions are met:
    /// <list type="bullet">
    ///   <item><description>ThicknessMm is not a finite number (NaN or Infinity)</description></item>
    ///   <item><description>ThicknessMm is less than or equal to zero</description></item>
    ///   <item><description>CoolingRateCPerSec is not a finite number (NaN or Infinity)</description></item>
    ///   <item><description>CoolingRateCPerSec is less than or equal to zero</description></item>
    ///   <item><description>CoolingRateCPerSec exceeds 200 °C/s (unrealistic for typical casting)</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// This validation ensures input data integrity before performing metallurgical calculations.
    /// The upper limit on cooling rate (200 °C/s) is a sanity check to catch potential unit
    /// conversion errors, as typical casting operations rarely exceed 50 °C/s.
    /// </remarks>
    public static void Validate(SectionProfile section)
    {
        // Null check
        if (section is null)
            throw new ArgumentNullException(nameof(section));

        // Validate thickness is a finite number
        if (double.IsNaN(section.ThicknessMm) || double.IsInfinity(section.ThicknessMm))
            throw new ArgumentException("ThicknessMm must be a finite number.", nameof(section));

        // Validate thickness is positive
        if (section.ThicknessMm <= 0)
            throw new ArgumentException("ThicknessMm must be > 0.", nameof(section));

        // Validate cooling rate is a finite number
        if (double.IsNaN(section.CoolingRateCPerSec) || double.IsInfinity(section.CoolingRateCPerSec))
            throw new ArgumentException("CoolingRateCPerSec must be a finite number.", nameof(section));

        // Validate cooling rate is positive
        if (section.CoolingRateCPerSec <= 0)
            throw new ArgumentException("CoolingRateCPerSec must be > 0 (°C/s).", nameof(section));

        // Sanity check: typical casting cooling rates are well below 50 °C/s
        // This upper limit helps catch unit conversion errors (e.g., entering 2000 instead of 2.0)
        if (section.CoolingRateCPerSec > CastIronInputConstraints.CoolingRateMax)
            throw new ArgumentException("CoolingRateCPerSec seems unrealistically high. Check units (expected °C/s).", nameof(section));
    }
}