using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Services;
using System;
using Xunit;

namespace Castara.Domain.Tests.Composition;

public sealed class CompositionTests
{
    [Theory]
    [InlineData(2.0, 2.2, 0.3, 0.05, 0.03)] // Carbon too low
    [InlineData(3.6, 0.1, 0.3, 0.05, 0.03)] // Silicon too low
    [InlineData(3.6, 2.2, 2.0, 0.05, 0.03)] // Manganese too high
    [InlineData(3.6, 2.2, 0.3, 0.9, 0.03)]  // Phosphorus too high
    [InlineData(3.6, 2.2, 0.3, 0.05, 0.9)]  // Sulfur too high
    public void Estimate_Throws_For_Each_OutOfRange_Field(
        double c, double si, double mn, double p, double s)
    {
        var sut = new CastIronEstimator();

        var inputs = new CastIronInputs(
            new CastIronComposition(c, si, mn, p, s),
            new SectionProfile(20, CoolingRate.Normal));

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Estimate(inputs));
    }
}