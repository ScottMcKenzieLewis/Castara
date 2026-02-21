namespace Castara.Application.UseCases.DetermineCastIronType;

using Castara.Application.DTOs;
using Castara.Application.Validation;

public sealed class DetermineCastIronTypeHandler
{
    public IronClassificationDto Handle(DetermineCastIronTypeRequest request)
    {
        var c = request.Composition;
        CompositionValidator.ValidateOrThrow(c);

        // Gray iron often higher C + Si; ductile often requires Mg treatment.
        if (request.MagnesiumTreated)
        {
            return new IronClassificationDto(
                Type: "Ductile",
                Confidence: "High",
                Rationale: "Magnesium treatment indicates nodular graphite potential."
            );
        }

        if (c.Carbon >= 3.0 && c.Silicon >= 1.5)
        {
            return new IronClassificationDto(
                "Gray",
                "Medium",
                "Carbon and silicon are in a typical gray iron range; no Mg treatment indicated."
            );
        }

        // White iron is typically lower Si (promotes carbides) — simplified heuristic
        if (c.Silicon < 1.0 && c.Carbon >= 2.5)
        {
            return new IronClassificationDto(
                "White",
                "Low",
                "Low silicon may promote carbide formation; classification is heuristic."
            );
        }

        return new IronClassificationDto("Unknown", "Low", "Composition does not match basic heuristics.");
    }
}