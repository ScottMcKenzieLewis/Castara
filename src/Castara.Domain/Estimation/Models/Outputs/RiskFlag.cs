using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Domain.Estimation.Models.Outputs;

public sealed record RiskFlag(
    string Code,
    string Name,
    RiskSeverity Severity,
    string Message);
