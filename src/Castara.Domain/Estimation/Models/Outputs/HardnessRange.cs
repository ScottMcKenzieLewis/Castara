namespace Castara.Domain.Estimation.Models.Outputs;

public readonly record struct HardnessRange(int MinHB, int MaxHB)
{
    public override string ToString() => $"{MinHB}-{MaxHB} HB";
}