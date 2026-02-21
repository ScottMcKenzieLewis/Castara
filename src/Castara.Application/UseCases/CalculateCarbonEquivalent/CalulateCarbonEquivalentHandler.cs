namespace Castara.Application.UseCases.CalculateCarbonEquivalent;

using Castara.Application.Validation;

public sealed class CalculateCarbonEquivalentHandler
{
    public double Handle(CalculateCarbonEquivalentRequest request)
    {
        var c = request.Composition;
        CompositionValidator.ValidateOrThrow(c);

        // Simple CE
        // CE = C + (Si / 3) + (P / 3)
        return c.Carbon + (c.Silicon / 3.0) + (c.Phosphorus / 3.0);
    }
}