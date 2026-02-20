namespace Castara.Domain.Estimation.Models.Outputs;

public sealed record CastIronEstimate(
    double CarbonEquivalent,
    double GraphitizationScore,
    HardnessRange EstimatedHardness,
    IReadOnlyList<RiskFlag> Flags);