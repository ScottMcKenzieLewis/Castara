namespace Castara.Domain.Composition;

public static class CompositionGuards
{
    public static void Validate(CastIronComposition c)
    {
        // Keep ranges broad; we’re not doing certified metallurgy.
        RequireRange(c.Carbon, 2.5, 4.5, nameof(c.Carbon));
        RequireRange(c.Silicon, 0.5, 3.5, nameof(c.Silicon));
        RequireRange(c.Manganese, 0.0, 1.5, nameof(c.Manganese));
        RequireRange(c.Phosphorus, 0.0, 0.3, nameof(c.Phosphorus));
        RequireRange(c.Sulfur, 0.0, 0.2, nameof(c.Sulfur));
    }

    private static void RequireRange(double value, double min, double max, string name)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");
    }
}