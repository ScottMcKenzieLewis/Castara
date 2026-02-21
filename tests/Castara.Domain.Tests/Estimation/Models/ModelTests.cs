using Castara.Domain.Composition;
using Castara.Domain.Estimation;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Xunit;

namespace Castara.Domain.Tests.Estimation;

public sealed class ModelTests
{
    [Fact]
    public void Models_Should_Be_Constructible_And_ToString_NotEmpty()
    {
        var comp = new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03);
        var section = new SectionProfile(20, CoolingRate.Normal);
        var inputs = new CastIronInputs(comp, section);

        var hardness = new HardnessRange(200, 230);
        var flag = new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "ok");

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 4.350,
            GraphitizationScore: 0.500,
            EstimatedHardness: hardness,
            Flags: new[] { flag });

        // Hits ToString overrides / record plumbing lines if present
        Assert.False(string.IsNullOrWhiteSpace(hardness.ToString()));

        // Touch properties to ensure getters are executed (coverage sometimes counts these)
        Assert.Equal(3.6, inputs.Composition.Carbon);
        Assert.Equal(CoolingRate.Normal, inputs.Section.CoolingRate);

        Assert.Equal("CHILL_RISK", estimate.Flags[0].Code);
        Assert.Equal(200, estimate.EstimatedHardness.MinHB);
    }

    [Fact]
    public void Records_With_Same_Values_Should_Be_Equal()
    {
        var a = new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03);
        var b = new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03);

        Assert.Equal(a, b);

        var s1 = new SectionProfile(20, CoolingRate.Normal);
        var s2 = new SectionProfile(20, CoolingRate.Normal);

        Assert.Equal(s1, s2);

        var f1 = new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "ok");
        var f2 = new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "ok");

        Assert.Equal(f1, f2);
    }

    [Fact]
    public void RiskFlag_RecordGeneratedMembers_Are_Valid()
    {
        var a = new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "ok");
        var b = new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "ok");
        var c = new RiskFlag("SHRINK_RISK", "Shrink/Porosity Risk", RiskSeverity.High, "warn");

        // Equality / operator path
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);

        // ToString / PrintMembers path
        var text = a.ToString();
        Assert.Contains("CHILL_RISK", text);

        // Deconstruct path
        var (code, name, sev, msg) = a;
        Assert.Equal("CHILL_RISK", code);
        Assert.Equal("Chill Risk", name);
        Assert.Equal(RiskSeverity.Low, sev);
        Assert.Equal("ok", msg);
    }

    [Fact]
    public void HardnessRange_RecordStruct_Members_Are_Valid()
    {
        var a = new HardnessRange(200, 220);
        var b = new HardnessRange(200, 220);
        var c = new HardnessRange(210, 230);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);

        Assert.Equal("200-220 HB", a.ToString());

        // Deconstruct for record struct
        var (min, max) = a;
        Assert.Equal(200, min);
        Assert.Equal(220, max);
    }

}