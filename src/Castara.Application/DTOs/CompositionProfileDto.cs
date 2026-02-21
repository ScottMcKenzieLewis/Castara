namespace Castara.Application.DTOs;

public sealed record CompositionProfileDto(
    Guid Id,
    string Name,
    CompositionDto Composition,
    DateTimeOffset CreatedAt
);