using Castara.Domain.Composition;

namespace Castara.Domain.Estimation.Validation;

/// <summary>
/// Provides validation guards for <see cref="CastIronComposition"/> instances to ensure
/// chemical element percentages are within realistic and safe ranges for cast iron analysis.
/// </summary>
/// <remarks>
/// <para>
/// These validation ranges are intentionally broad to accommodate a wide variety of cast iron
/// compositions encountered in practice, from low-strength gray iron to higher-strength variants.
/// The guards are designed to catch data entry errors and unrealistic values rather than
/// enforce strict metallurgical specifications.
/// </para>
/// <para>
/// <strong>Note:</strong> This validation is for estimation purposes and does not represent
/// certified metallurgical specifications or industry standards for production quality control.
/// Always consult appropriate standards (ASTM, SAE, etc.) for specification compliance.
/// </para>
/// </remarks>
public static class CompositionGuards
{
    /// <summary>
    /// Validates a <see cref="CastIronComposition"/> instance, ensuring all element percentages
    /// are within realistic ranges for cast iron production and analysis.
    /// </summary>
    /// <param name="c">The composition to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any element percentage is outside its acceptable range:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Element</term>
    ///     <description>Acceptable Range (wt%)</description>
    ///   </listheader>
    ///   <item>
    ///     <term>Carbon</term>
    ///     <description>2.5 - 4.5%</description>
    ///   </item>
    ///   <item>
    ///     <term>Silicon</term>
    ///     <description>0.5 - 3.5%</description>
    ///   </item>
    ///   <item>
    ///     <term>Manganese</term>
    ///     <description>0.0 - 1.5%</description>
    ///   </item>
    ///   <item>
    ///     <term>Phosphorus</term>
    ///     <description>0.0 - 0.3%</description>
    ///   </item>
    ///   <item>
    ///     <term>Sulfur</term>
    ///     <description>0.0 - 0.2%</description>
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// These ranges encompass typical cast iron compositions from Class 20 to Class 60 gray iron,
    /// as well as some specialty irons. Values outside these ranges likely indicate:
    /// <list type="bullet">
    ///   <item><description>Data entry errors (decimal point misplacement, unit confusion)</description></item>
    ///   <item><description>Non-cast iron materials (steel, wrought iron, etc.)</description></item>
    ///   <item><description>Unrealistic or experimental compositions</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The ranges are intentionally broader than typical production specifications to
    /// accommodate exploration and "what-if" analysis scenarios while still catching
    /// obvious errors.
    /// </para>
    /// </remarks>
    public static void Validate(CastIronComposition c)
    {
        // Keep ranges broad; we're not doing certified metallurgy
        RequireRange(c.Carbon, 2.5, 4.5, nameof(c.Carbon));
        RequireRange(c.Silicon, 0.5, 3.5, nameof(c.Silicon));
        RequireRange(c.Manganese, 0.0, 1.5, nameof(c.Manganese));
        RequireRange(c.Phosphorus, 0.0, 0.3, nameof(c.Phosphorus));
        RequireRange(c.Sulfur, 0.0, 0.2, nameof(c.Sulfur));
    }

    /// <summary>
    /// Validates that a numeric value falls within a specified range, throwing an exception if out of bounds.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum acceptable value (inclusive).</param>
    /// <param name="max">The maximum acceptable value (inclusive).</param>
    /// <param name="name">The name of the parameter being validated, used in the exception message.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is less than <paramref name="min"/> or greater than <paramref name="max"/>.
    /// The exception message includes the parameter name, actual value, and acceptable range.
    /// </exception>
    private static void RequireRange(double value, double min, double max, string name)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");
    }
}