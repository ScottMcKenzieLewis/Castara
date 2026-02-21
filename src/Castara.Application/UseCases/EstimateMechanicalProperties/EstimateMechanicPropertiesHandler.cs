namespace Castara.Application.UseCases.EstimateMechanicalProperties;

using Castara.Application.Abstractions.Services;
using Castara.Application.DTOs;
using Castara.Application.Validation;

public sealed class EstimateMechanicalPropertiesHandler
{
    private readonly IPropertyCalculator _calculator;

    public EstimateMechanicalPropertiesHandler(IPropertyCalculator calculator)
    {
        _calculator = calculator;
    }

    public PropertyResultDto Handle(EstimateMechanicalPropertiesRequest request)
    {
        CompositionValidator.ValidateOrThrow(request.Composition);
        return _calculator.Estimate(request.Composition);
    }
}