namespace Castara.Application.UseCases.DetermineCastIronType;

using Castara.Application.DTOs;

public sealed record DetermineCastIronTypeRequest(
    CompositionDto Composition,
    bool MagnesiumTreated // a user-provided clue for ductile iron
);