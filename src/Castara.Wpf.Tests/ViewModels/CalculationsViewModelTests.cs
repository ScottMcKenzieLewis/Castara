using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;

using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.ViewModels;

using Moq;

using OxyPlot.Series;

using Xunit;

namespace Castara.Wpf.Tests;

public sealed class CalculationsViewModelTests
{
    // -----------------------------
    // Spies / helpers
    // -----------------------------

    private sealed class StatusSpy
    {
        public Mock<IStatusService> Mock { get; }
        public List<(AppStatusLevel Level, string Left, string Right)> Calls { get; } = new();

        private StatusState _current = new(AppStatusLevel.Ok, "Ready", "Ready for Calculation");

        public StatusSpy(MockBehavior behavior)
        {
            Mock = new Mock<IStatusService>(behavior);

            Mock.SetupGet(s => s.Current).Returns(() => _current);

            Mock.Setup(s => s.Set(It.IsAny<AppStatusLevel>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<AppStatusLevel, string, string>((lvl, left, right) =>
                {
                    _current = new StatusState(lvl, left, right);
                    Calls.Add((lvl, left, right));
                });

            // VM doesn’t need status PropertyChanged today, but keep it safe.
            Mock.SetupAdd(s => s.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());
        }
    }

    private static (CalculationsViewModel Vm, StatusSpy Status, Mock<ICastIronEstimator> Estimator) CreateSut(
        MockBehavior behavior = MockBehavior.Strict)
    {
        var status = new StatusSpy(behavior);
        var estimator = new Mock<ICastIronEstimator>(behavior);

        var vm = new CalculationsViewModel(status.Mock.Object, estimator.Object);
        return (vm, status, estimator);
    }

    private static BarSeries GetCompositionSeries(CalculationsViewModel vm)
        => Assert.IsType<BarSeries>(Assert.Single(vm.CompositionPlotModel.Series));

    private static PieSeries GetGraphGaugeSeries(CalculationsViewModel vm)
        => Assert.IsType<PieSeries>(Assert.Single(vm.GraphGaugeModel.Series));

    private static PieSeries GetHardnessGaugeSeries(CalculationsViewModel vm)
        => Assert.IsType<PieSeries>(Assert.Single(vm.HardnessGaugeModel.Series));

    private static double SliceValue(PieSeries s, int index) => s.Slices[index].Value;

    // -----------------------------
    // Tests
    // -----------------------------

    [Fact]
    public void Ctor_Sets_DefaultInputs_SeedsPlots_And_Sets_Ready_Status()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        // ctor should NOT call estimator
        estimator.VerifyNoOtherCalls();

        // Defaults (from your ctor)
        Assert.Equal(3.40, vm.Carbon, 3);
        Assert.Equal(2.10, vm.Silicon, 3);
        Assert.Equal(0.55, vm.Manganese, 3);
        Assert.Equal(0.05, vm.Phosphorus, 3);
        Assert.Equal(0.02, vm.Sulfur, 3);
        Assert.Equal(12.0, vm.ThicknessMm, 3);
        Assert.Equal(1.0, vm.CoolingRateCPerSec, 3);

        // No result initially
        Assert.False(vm.HasResult);
        Assert.Null(vm.Result);
        Assert.Equal("—", vm.CarbonEquivalentText);
        Assert.Equal("—", vm.GraphitizationScoreText);
        Assert.Equal("—", vm.HardnessText);
        Assert.Equal("—", vm.CoolingFactorText);
        Assert.Equal("—", vm.ThicknessFactorText);
        Assert.Empty(vm.Flags);

        // Composition plot seeded with defaults (and clamped display)
        var comp = GetCompositionSeries(vm);
        Assert.Equal(5, comp.Items.Count);
        Assert.Equal(3.40, comp.Items[0].Value, 3); // C
        Assert.Equal(2.10, comp.Items[1].Value, 3); // Si
        Assert.Equal(0.55, comp.Items[2].Value, 3); // Mn
        Assert.Equal(0.05, comp.Items[3].Value, 3); // P
        Assert.Equal(0.02, comp.Items[4].Value, 3); // S

        // Gauges seeded to zero (value slice 0, remainder 1)
        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(2, g.Slices.Count);
        Assert.Equal(0.0, SliceValue(g, 0), 10);
        Assert.Equal(1.0, SliceValue(g, 1), 10);

        var h = GetHardnessGaugeSeries(vm);
        Assert.Equal(2, h.Slices.Count);
        Assert.Equal(0.0, SliceValue(h, 0), 10);
        Assert.Equal(1.0, SliceValue(h, 1), 10);

        // Status set to Ready
        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Ok &&
            c.Left == "Ready" &&
            c.Right == "Ready for Calculation");
    }

    [Fact]
    public void Composition_Setters_Update_BarSeries_And_Clamp_Display_Values()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Loose);

        var comp = GetCompositionSeries(vm);

        vm.Carbon = 99;      // clamp to 5 in chart
        vm.Manganese = -10;  // clamp to 0 in chart
        vm.Phosphorus = 2;   // clamp to 1 in chart
        vm.Sulfur = double.NaN; // clamp to min (0) in chart

        Assert.Equal(5.0, comp.Items[0].Value, 10);
        Assert.Equal(0.0, comp.Items[2].Value, 10);
        Assert.Equal(1.0, comp.Items[3].Value, 10);
        Assert.Equal(0.0, comp.Items[4].Value, 10);

        // ctor status call exists; we don't care here
        Assert.True(status.Calls.Count >= 1);
        estimator.VerifyNoOtherCalls();
    }

    [Fact]
    public void Calculate_When_InvalidInputs_Clears_Result_Sets_Warning_And_Resets_Gauges()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        // Make invalid
        vm.Carbon = -1;

        vm.CalculateCommand.Execute(null);

        estimator.Verify(e => e.Estimate(It.IsAny<CastIronInputs>()), Times.Never);

        Assert.False(vm.HasResult);
        Assert.Null(vm.Result);

        // Should set warning with "Check inputs" and first issue as right text
        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Warning &&
            c.Left == "Check inputs" &&
            !string.IsNullOrWhiteSpace(c.Right));

        // Gauges should be reset to 0
        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(0.0, SliceValue(g, 0), 10);

        var h = GetHardnessGaugeSeries(vm);
        Assert.Equal(0.0, SliceValue(h, 0), 10);
    }

    [Fact]
    public void Calculate_When_Estimate_Succeeds_NoFlags_Sets_Result_Updates_Text_And_Status_Ok()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 3.8123,
            GraphitizationScore: 0.61234,
            EstimatedHardness: new HardnessRange(180, 220),
            CoolingFactor: 0.001,
            ThicknessFactor: -0.250,
            Flags: Array.Empty<RiskFlag>());

        estimator.Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
                 .Returns(estimate);

        vm.CalculateCommand.Execute(null);

        estimator.Verify(e => e.Estimate(It.IsAny<CastIronInputs>()), Times.Once);

        Assert.True(vm.HasResult);
        Assert.NotNull(vm.Result);
        Assert.Equal("3.812", vm.CarbonEquivalentText);
        Assert.Equal("0.612", vm.GraphitizationScoreText);
        Assert.Equal("180-220 HB", vm.HardnessText);
        Assert.Equal("0.001", vm.CoolingFactorText);
        Assert.Equal("-0.250", vm.ThicknessFactorText);
        Assert.Empty(vm.Flags);

        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Ok &&
            c.Left == "Calculated" &&
            c.Right == "No risks");

        // Gauges should reflect values
        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(0.61234, SliceValue(g, 0), 6);

        // Hardness normalization uses hb midpoint (200) in [140..320]
        // norm = (200-140)/(320-140)=60/180=0.333333...
        var h = GetHardnessGaugeSeries(vm);
        Assert.Equal(60.0 / 180.0, SliceValue(h, 0), 6);
    }

    [Fact]
    public void Calculate_When_HighSeverity_Flag_Sets_Status_Warning_And_RiskCount()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        var flags = new[]
        {
            new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.High, "High risk")
        };

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 3.9,
            GraphitizationScore: 0.4,
            EstimatedHardness: new HardnessRange(240, 280),
            CoolingFactor: 0.0,
            ThicknessFactor: 0.0,
            Flags: flags);

        estimator.Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
                 .Returns(estimate);

        vm.CalculateCommand.Execute(null);

        Assert.True(vm.HasResult);
        Assert.Single(vm.Flags);

        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Warning &&
            c.Left == "Calculated" &&
            c.Right == "1 risk(s)");
    }

    [Fact]
    public void Calculate_When_Estimator_Throws_Sets_Error_Status_And_Clears_Result()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        estimator.Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
                 .Throws(new InvalidOperationException("boom"));

        vm.CalculateCommand.Execute(null);

        Assert.False(vm.HasResult);
        Assert.Null(vm.Result);

        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Error &&
            c.Left == "Calculation failed" &&
            c.Right.Contains("boom", StringComparison.OrdinalIgnoreCase));

        // Gauges should be reset
        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(0.0, SliceValue(g, 0), 10);
    }

    [Fact]
    public void ClearCommand_Clears_Result_Resets_Gauges_And_Sets_Ready_Status()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        estimator.Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
                 .Returns(new CastIronEstimate(
                     CarbonEquivalent: 3.8,
                     GraphitizationScore: 0.7,
                     EstimatedHardness: new HardnessRange(170, 210),
                     CoolingFactor: 0.0,
                     ThicknessFactor: 0.0,
                     Flags: Array.Empty<RiskFlag>()));

        vm.CalculateCommand.Execute(null);
        Assert.True(vm.HasResult);

        vm.ClearCommand.Execute(null);

        Assert.False(vm.HasResult);
        Assert.Null(vm.Result);

        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(0.0, SliceValue(g, 0), 10);

        Assert.Contains(status.Calls, c =>
            c.Level == AppStatusLevel.Ok &&
            c.Left == "Ready" &&
            c.Right == "Ready for Calculation");
    }

    [Fact]
    public void SetTheme_Rebuilds_Plots_But_Preserves_LastGaugeValues()
    {
        var (vm, status, estimator) = CreateSut(MockBehavior.Strict);

        // Create a result so we have cached gauge values to repaint after theme change
        estimator.Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
                 .Returns(new CastIronEstimate(
                     CarbonEquivalent: 3.7,
                     GraphitizationScore: 0.25,              // should show on graph gauge
                     EstimatedHardness: new HardnessRange(300, 320), // midpoint 310 => norm (310-140)/180 = 0.944444...
                     CoolingFactor: 0.0,
                     ThicknessFactor: 0.0,
                     Flags: Array.Empty<RiskFlag>()));

        vm.CalculateCommand.Execute(null);

        var oldComp = vm.CompositionPlotModel;
        var oldGraph = vm.GraphGaugeModel;
        var oldHard = vm.HardnessGaugeModel;

        // Act
        vm.SetTheme(isDark: false);

        Assert.False(vm.IsDarkTheme);

        // Models should be rebuilt (new instances)
        Assert.NotSame(oldComp, vm.CompositionPlotModel);
        Assert.NotSame(oldGraph, vm.GraphGaugeModel);
        Assert.NotSame(oldHard, vm.HardnessGaugeModel);

        // Gauge values should be preserved after theme rebuild
        var g = GetGraphGaugeSeries(vm);
        Assert.Equal(0.25, SliceValue(g, 0), 6);

        var h = GetHardnessGaugeSeries(vm);
        var expectedHbNorm = (310.0 - 140.0) / (320.0 - 140.0); // 170/180 = 0.944444...
        Assert.Equal(expectedHbNorm, SliceValue(h, 0), 6);
    }
}