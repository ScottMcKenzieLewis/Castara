namespace Castara.Application.Abstractions.Services;

using Castara.Application.DTOs;

public interface IPropertyCalculator
{
    PropertyResultDto Estimate(CompositionDto composition);
}