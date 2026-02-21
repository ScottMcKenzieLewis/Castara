namespace Castara.Application.Validation;

using Castara.Application.DTOs;

public static class CompositionValidator
{
    public static void ValidateOrThrow(CompositionDto c)
    {
        // “Low/mid interview friendly” validation:
        // 1) no negatives
        // 2) plausible ranges
        EnsureRange(c.Carbon, 0, 10, nameof(c.Carbon));
        EnsureRange(c.Silicon, 0, 10, nameof(c.Silicon));
        EnsureRange(c.Manganese, 0, 5, nameof(c.Manganese));
        EnsureRange(c.Phosphorus, 0, 2, nameof(c.Phosphorus));
        EnsureRange(c.Sulfur, 0, 2, nameof(c.Sulfur));
        EnsureRange(c.Chromium, 0, 5, nameof(c.Chromium));
        EnsureRange(c.Nickel, 0, 10, nameof(c.Nickel));
        EnsureRange(c.Molybdenum, 0, 5, nameof(c.Molybdenum));
    }

    private static void EnsureRange(double value, double min, double max, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException($"{name} is invalid.");

        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between {min} and {max}.");
    }
}