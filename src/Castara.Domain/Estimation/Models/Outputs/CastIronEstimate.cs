namespace Castara.Domain.Estimation.Models.Outputs;

public sealed record CastIronEstimate(
    double CarbonEquivalent,
    double GraphitizationScore,
    HardnessRange EstimatedHardness,
    double CoolingFactor,
    double ThicknessFactor,
    IReadOnlyList<RiskFlag> Flags);