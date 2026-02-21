namespace Castara.Application.UseCases.CompareCompositions;

using Castara.Application.DTOs;
using Castara.Application.Validation;

public sealed class CompareCompositionsHandler
{
    public CompositionDiffDto Handle(CompareCompositionsRequest request)
    {
        CompositionValidator.ValidateOrThrow(request.A);
        CompositionValidator.ValidateOrThrow(request.B);

        var deltas = new List<(string, double)>
        {
            ("C",  request.B.Carbon - request.A.Carbon),
            ("Si", request.B.Silicon - request.A.Silicon),
            ("Mn", request.B.Manganese - request.A.Manganese),
            ("P",  request.B.Phosphorus - request.A.Phosphorus),
            ("S",  request.B.Sulfur - request.A.Sulfur),
            ("Cr", request.B.Chromium - request.A.Chromium),
            ("Ni", request.B.Nickel - request.A.Nickel),
            ("Mo", request.B.Molybdenum - request.A.Molybdenum),
        };

        return new CompositionDiffDto(request.A, request.B, deltas);
    }
}