using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Domain.Estimation.Validation;

public static class SectionGuards
{
    // Keep these broad; this is a planning tool, not a certified model.
    private const double MinThicknessMm = 2.0;
    private const double MaxThicknessMm = 200.0;

    public static void Validate(SectionProfile section)
    {
        RequireRange(section.ThicknessMm, MinThicknessMm, MaxThicknessMm, nameof(section.ThicknessMm));
    }

    private static void RequireRange(double value, double min, double max, string name)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");
    }
}