namespace Castara.Infrastructure.Persistence.Entities;

public sealed class CompositionProfileEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public double Carbon { get; set; }
    public double Silicon { get; set; }
    public double Manganese { get; set; }
    public double Phosphorus { get; set; }
    public double Sulfur { get; set; }
    public double Chromium { get; set; }
    public double Nickel { get; set; }
    public double Molybdenum { get; set; }
}