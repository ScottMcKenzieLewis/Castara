using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using OxyPlot.Series;

namespace Castara.Wpf.Tests.ViewModels;

public sealed class CalculationsViewModelTests
{
    private static CalculationsViewModel CreateVm(
        out Mock<IStatusService> statusMock,
        out Mock<ICastIronEstimator> estimatorMock,
        out Mock<ILogger<CalculationsViewModel>> loggerMock)
    {
        statusMock = new Mock<IStatusService>(MockBehavior.Strict);
        estimatorMock = new Mock<ICastIronEstimator>(MockBehavior.Strict);
        loggerMock = new Mock<ILogger<CalculationsViewModel>>(MockBehavior.Loose);

        // You can keep Strict on status/estimator and Loose on logger.
        // Strict helps catch accidental extra calls.

        statusMock
            .Setup(s => s.Set(It.IsAny<AppStatusLevel>(), It.IsAny<string>(), It.IsAny<string>()));

        return new CalculationsViewModel(
            statusMock.Object,
            estimatorMock.Object,
            loggerMock.Object);
    }

    // ============================================================
    // Constructor / Initialization
    // ============================================================

    [Fact]
    public void Constructor_InitializesDefaults_AndPlots()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        Assert.Equal(3.40, vm.Carbon, 3);
        Assert.Equal(2.10, vm.Silicon, 3);
        Assert.Equal(12.0, vm.ThicknessMm, 3);

        Assert.NotNull(vm.CompositionPlotModel);
        Assert.NotNull(vm.GraphGaugeModel);
        Assert.NotNull(vm.HardnessGaugeModel);

        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Ok,
                  "Ready",
                  "Ready for Calculation"),
            Times.Once);
    }

    // ============================================================
    // Validation behavior
    // ============================================================

    [Fact]
    public void InvalidText_DoesNotUpdateCanonicalValue()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        var before = vm.Carbon;

        vm.CarbonText = "abc";

        Assert.Equal(before, vm.Carbon);
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void OutOfRangeText_FailsValidation()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        vm.CarbonText = "10";

        Assert.False(vm.IsValid);

        var err =
            ((System.ComponentModel.IDataErrorInfo)vm)
            [nameof(vm.CarbonText)];

        Assert.Contains("between", err);
    }

    [Fact]
    public void ValidText_UpdatesCanonicalValue()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        vm.CarbonText = "4.0";

        Assert.Equal(4.0, vm.Carbon, 6);
        Assert.True(vm.IsValid);
    }

    // ============================================================
    // Calculate Command
    // ============================================================

    [Fact]
    public void CalculateCommand_CallsEstimator_WithCanonicalInputs()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        vm.CalculateCommand.Execute(null);

        estimatorMock.Verify(e =>
            e.Estimate(It.Is<CastIronInputs>(i =>
                i.Composition.Carbon == vm.Carbon &&
                i.Section.ThicknessMm == vm.ThicknessMm)),
            Times.Once);
    }

    [Fact]
    public void CalculateCommand_SetsResult_AndStatusOk()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        vm.CalculateCommand.Execute(null);

        Assert.NotNull(vm.Result);

        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Ok,
                  "Calculated",
                  "OK"),
            Times.Once);
    }

    [Fact]
    public void CalculateCommand_InvalidInputs_DoesNotCallEstimator()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        vm.CarbonText = "abc";

        vm.CalculateCommand.Execute(null);

        estimatorMock.Verify(
            e => e.Estimate(It.IsAny<CastIronInputs>()),
            Times.Never);
    }

    [Fact]
    public void CalculateCommand_Exception_SetsErrorStatus()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Throws(new InvalidOperationException("boom"));

        vm.CalculateCommand.Execute(null);

        Assert.Null(vm.Result);

        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Error,
                  "Calculation failed",
                  "boom"),
            Times.Once);
    }

    // ============================================================
    // Clear Command
    // ============================================================

    [Fact]
    public void ClearCommand_ResetsDefaults_AndResult()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        vm.CalculateCommand.Execute(null);

        Assert.NotNull(vm.Result);

        vm.ClearCommand.Execute(null);

        Assert.Null(vm.Result);

        Assert.Equal(3.40, vm.Carbon, 3);
        Assert.Equal(12.0, vm.ThicknessMm, 3);
    }

    // ============================================================
    // Composition Plot Behavior
    // ============================================================

    [Fact]
    public void ChangingComposition_UpdatesPlotValues()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        vm.CarbonText = "4.0";

        var barSeries =
            vm.CompositionPlotModel.Series
            .OfType<BarSeries>()
            .Single();

        Assert.Equal(4.0, barSeries.Items[0].Value, 6);
    }

    // ============================================================
    // Theme Behavior
    // ============================================================

    [Fact]
    public void ChangingTheme_RebuildsPlotModels()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out var loggerMock);

        var before = vm.CompositionPlotModel;

        vm.IsDarkTheme = !vm.IsDarkTheme;

        Assert.NotSame(before, vm.CompositionPlotModel);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static CastIronEstimate CreateEstimate()
    {
        return new CastIronEstimate(
            CarbonEquivalent: 4.2,
            GraphitizationScore: 0.6,
            CoolingFactor: 0.1,
            ThicknessFactor: -0.05,
            EstimatedHardness: new HardnessRange(180, 220),
            Flags: new[]
            {
                new RiskFlag("CHILL_RISK","Chill",RiskSeverity.Low, "msg"),
                new RiskFlag("SHRINK_RISK","Shrink",RiskSeverity.Low, "msg"),
                new RiskFlag("MACHINABILITY","Mach",RiskSeverity.Low, "msg")
            });
    }
}