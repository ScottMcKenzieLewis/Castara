namespace Castara.Application.UseCases.SaveCompositionProfile;

using Castara.Application.DTOs;

public sealed record SaveCompositionProfileRequest(
    string Name,
    CompositionDto Composition
);