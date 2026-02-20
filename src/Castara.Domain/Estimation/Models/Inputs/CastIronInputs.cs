using Castara.Domain.Composition;

namespace Castara.Domain.Estimation.Models.Inputs;

public sealed record CastIronInputs(
    CastIronComposition Composition,
    SectionProfile Section);