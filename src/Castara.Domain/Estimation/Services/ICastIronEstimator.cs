using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Domain.Estimation.Services;

public interface ICastIronEstimator
{
    CastIronEstimate Estimate(CastIronInputs inputs);
}