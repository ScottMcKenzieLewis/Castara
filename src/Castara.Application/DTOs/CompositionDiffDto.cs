namespace Castara.Application.DTOs;

public sealed record CompositionDiffDto(
    CompositionDto A,
    CompositionDto B,
    IReadOnlyList<(string Element, double Delta)> Deltas
);