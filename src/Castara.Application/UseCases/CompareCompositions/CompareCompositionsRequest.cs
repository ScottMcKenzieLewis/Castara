namespace Castara.Application.UseCases.CompareCompositions;

using Castara.Application.DTOs;

public sealed record CompareCompositionsRequest(CompositionDto A, CompositionDto B);