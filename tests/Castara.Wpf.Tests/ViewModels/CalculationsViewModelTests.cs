using System;
using System.Linq;
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
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="CalculationsViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the calculations view model's core responsibilities:
/// <list type="bullet">
///   <item><description>Initialization with sensible defaults (typical Class 30 gray iron)</description></item>
///   <item><description>Real-time text-based validation with <see cref="System.ComponentModel.IDataErrorInfo"/></description></item>
///   <item><description>Unit system conversion between Standard (SI) and American Standard units</description></item>
///   <item><description>Calculation execution with proper error handling and status reporting</description></item>
///   <item><description>Chart visualization updates (composition, gauges) driven by input changes</description></item>
///   <item><description>Theme coordination for OxyPlot visualizations</description></item>
///   <item><description>Clear/reset functionality returning to default state</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Testing Strategy:</strong> The view model separates text input (user-facing) from
/// canonical numeric values (domain-facing). Tests verify that:
/// <list type="bullet">
///   <item><description>Invalid text does not corrupt canonical values</description></item>
///   <item><description>Valid text updates both display and canonical values</description></item>
///   <item><description>Unit conversions preserve canonical SI values while updating display</description></item>
///   <item><description>Calculations always use canonical SI values regardless of display units</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Mock Strategy:</strong> Status service uses strict behavior to verify status updates.
/// Estimator uses strict behavior to verify calculation inputs. Logger uses loose behavior
/// to avoid test brittleness from logging changes.
/// </para>
/// </remarks>
public sealed class CalculationsViewModelTests
{
    // ============================================================
    // Test Helpers - View Model Factory
    // ============================================================

    /// <summary>
    /// Creates a <see cref="CalculationsViewModel"/> instance with all dependencies mocked.
    /// </summary>
    /// <param name="statusMock">The mocked status service for verifying status updates.</param>
    /// <param name="estimatorMock">The mocked cast iron estimator for verifying calculation calls.</param>
    /// <param name="loggerMock">The mocked logger (loose behavior for flexibility).</param>
    /// <returns>A fully initialized calculations view model ready for testing.</returns>
    /// <remarks>
    /// <para>
    /// This factory method sets up all required mocks with appropriate behaviors:
    /// <list type="bullet">
    ///   <item><description>Status service accepts any status updates (strict mock)</description></item>
    ///   <item><description>Estimator requires explicit setup for each test scenario</description></item>
    ///   <item><description>Logger uses loose behavior to avoid test brittleness</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// During construction, the view model:
    /// <list type="bullet">
    ///   <item><description>Sets default composition (C: 3.40%, Si: 2.10%, Mn: 0.55%, P: 0.05%, S: 0.02%)</description></item>
    ///   <item><description>Sets default section parameters (Thickness: 12mm, Cooling: 1°C/s)</description></item>
    ///   <item><description>Initializes plot models for composition bars and result gauges</description></item>
    ///   <item><description>Sets status to "Ready"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static CalculationsViewModel CreateVm(
        out Mock<IStatusService> statusMock,
        out Mock<ICastIronEstimator> estimatorMock,
        out Mock<ILogger<CalculationsViewModel>> loggerMock)
    {
        statusMock = new Mock<IStatusService>(MockBehavior.Strict);
        estimatorMock = new Mock<ICastIronEstimator>(MockBehavior.Strict);
        loggerMock = new Mock<ILogger<CalculationsViewModel>>(MockBehavior.Loose);

        statusMock
            .Setup(s => s.Set(It.IsAny<AppStatusLevel>(), It.IsAny<string>(), It.IsAny<string>()));

        return new CalculationsViewModel(
            statusMock.Object,
            estimatorMock.Object,
            loggerMock.Object);
    }

    // ============================================================
    // Test Helpers - Test Data Factories
    // ============================================================

    /// <summary>
    /// Creates a test <see cref="CastIronEstimate"/> with typical result values.
    /// </summary>
    /// <returns>A configured estimate with realistic values and sample risk flags.</returns>
    /// <remarks>
    /// This factory creates an estimate representing a typical gray iron calculation result:
    /// <list type="bullet">
    ///   <item><description>Carbon Equivalent: 4.2%</description></item>
    ///   <item><description>Graphitization Score: 0.6 (moderate graphite formation tendency)</description></item>
    ///   <item><description>Hardness Range: 180-220 HB (typical for Class 30)</description></item>
    ///   <item><description>Three low-severity risk flags for test verification</description></item>
    /// </list>
    /// </remarks>
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
                new RiskFlag("CHILL_RISK", "Chill", RiskSeverity.Low, "msg"),
                new RiskFlag("SHRINK_RISK", "Shrink", RiskSeverity.Low, "msg"),
                new RiskFlag("MACHINABILITY", "Mach", RiskSeverity.Low, "msg")
            });
    }

    // ============================================================
    // Tests - Constructor and Initialization
    // ============================================================

    /// <summary>
    /// Verifies that the constructor initializes default composition values, section parameters,
    /// plot models, and sets the initial status to "Ready".
    /// </summary>
    [Fact]
    public void Constructor_InitializesDefaults_AndPlots()
    {
        var vm = CreateVm(out var statusMock, out _, out _);

        // Verify default composition (typical Class 30 gray iron)
        Assert.Equal(3.40, vm.Carbon, 3);
        Assert.Equal(2.10, vm.Silicon, 3);

        // Verify default section parameters
        Assert.Equal(12.0, vm.ThicknessValue, 3);
        Assert.Equal(1.0, vm.CoolingRateValue, 3);

        // Verify plot models are initialized
        Assert.NotNull(vm.CompositionPlotModel);
        Assert.NotNull(vm.GraphGaugeModel);
        Assert.NotNull(vm.HardnessGaugeModel);

        // Verify initial status
        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"),
            Times.Once);
    }

    // ============================================================
    // Tests - Validation - Invalid Input Handling
    // ============================================================

    /// <summary>
    /// Verifies that unparseable text does not corrupt the canonical numeric value.
    /// </summary>
    [Fact]
    public void InvalidText_DoesNotUpdateCanonicalValue()
    {
        var vm = CreateVm(out _, out _, out _);

        var before = vm.Carbon;

        vm.CarbonText = "abc";

        Assert.Equal(before, vm.Carbon);
        Assert.False(vm.IsValid);
    }

    /// <summary>
    /// Verifies that out-of-range numeric text fails validation and provides appropriate error messages.
    /// </summary>
    [Fact]
    public void OutOfRangeText_FailsValidation()
    {
        var vm = CreateVm(out _, out _, out _);

        vm.CarbonText = "10";

        Assert.False(vm.IsValid);

        var err =
            ((System.ComponentModel.IDataErrorInfo)vm)
            [nameof(vm.CarbonText)];

        Assert.Contains("between", err);
    }

    // ============================================================
    // Tests - Validation - Valid Input Handling
    // ============================================================

    /// <summary>
    /// Verifies that valid text updates the canonical numeric value and passes validation.
    /// </summary>
    [Fact]
    public void ValidText_UpdatesCanonicalValue()
    {
        var vm = CreateVm(out _, out _, out _);

        vm.CarbonText = "4.0";

        Assert.Equal(4.0, vm.Carbon, 6);
        Assert.True(vm.IsValid);
    }

    // ============================================================
    // Tests - Unit System - Display Conversion
    // ============================================================

    /// <summary>
    /// Verifies that changing the unit system re-seeds display text from canonical SI values
    /// using appropriate unit conversions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the critical separation between canonical (SI) and display values:
    /// <list type="bullet">
    ///   <item><description>Canonical values remain in SI units (mm, °C/s)</description></item>
    ///   <item><description>Display text updates with converted values (inches, °F/s)</description></item>
    ///   <item><description>Conversions: 12mm → 0.472in, 1°C/s → 1.8°F/s</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [Fact]
    public void ChangingUnitSystem_ReseedsDisplayText_FromCanonicalValues()
    {
        var vm = CreateVm(out _, out _, out _);

        // Defaults are canonical: ThicknessValue=12mm, CoolingRateValue=1°C/s
        vm.UnitSystem = UnitSystem.AmericanStandard;

        // 12mm → 0.47244in, formatted "0.###" => "0.472"
        Assert.Equal("0.472", vm.ThicknessText);

        // 1°C/s → 1.8°F/s, formatted "0.####" => "1.8"
        Assert.Equal("1.8", vm.CoolingRateText);

        Assert.True(vm.IsValid);
    }

    // ============================================================
    // Tests - Unit System - Input Conversion
    // ============================================================

    /// <summary>
    /// Verifies that text input in American Standard units correctly updates canonical SI values
    /// through inverse unit conversion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies bidirectional unit conversion:
    /// <list type="bullet">
    ///   <item><description>User enters values in American Standard units (inches, °F/s)</description></item>
    ///   <item><description>View model converts and stores in canonical SI units</description></item>
    ///   <item><description>Calculations always use SI values: 1.0in → 25.4mm, 1.8°F/s → 1.0°C/s</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [Fact]
    public void AmericanStandard_DisplayInput_UpdatesCanonicalSiValues()
    {
        var vm = CreateVm(out _, out _, out _);

        vm.UnitSystem = UnitSystem.AmericanStandard;

        vm.ThicknessText = "1.0";     // inches
        vm.CoolingRateText = "1.8";   // °F/s

        Assert.Equal(25.4, vm.ThicknessValue, 6);   // mm
        Assert.Equal(1.0, vm.CoolingRateValue, 6);  // °C/s
    }

    // ============================================================
    // Tests - Calculate Command - Execution
    // ============================================================

    /// <summary>
    /// Verifies that the Calculate command calls the estimator with canonical SI input values
    /// regardless of the current display unit system.
    /// </summary>
    [Fact]
    public void CalculateCommand_CallsEstimator_WithCanonicalInputs()
    {
        var vm = CreateVm(out _, out var estimatorMock, out _);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        vm.CalculateCommand.Execute(null);

        estimatorMock.Verify(e =>
            e.Estimate(It.Is<CastIronInputs>(i =>
                i.Composition.Carbon == vm.Carbon &&
                i.Section.ThicknessMm == vm.ThicknessValue &&
                i.Section.CoolingRateCPerSec == vm.CoolingRateValue)),
            Times.Once);
    }

    /// <summary>
    /// Verifies that successful calculation updates the result and sets status to "Calculated".
    /// </summary>
    [Fact]
    public void CalculateCommand_SetsResult_AndStatusOk()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out _);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        vm.CalculateCommand.Execute(null);

        Assert.NotNull(vm.Result);

        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Ok, "Calculated", "OK"),
            Times.Once);
    }

    // ============================================================
    // Tests - Calculate Command - Validation Guard
    // ============================================================

    /// <summary>
    /// Verifies that the Calculate command does not call the estimator when inputs are invalid,
    /// preventing execution with bad data.
    /// </summary>
    [Fact]
    public void CalculateCommand_InvalidInputs_DoesNotCallEstimator()
    {
        var vm = CreateVm(out _, out var estimatorMock, out _);

        vm.CarbonText = "abc";

        vm.CalculateCommand.Execute(null);

        estimatorMock.Verify(
            e => e.Estimate(It.IsAny<CastIronInputs>()),
            Times.Never);
    }

    // ============================================================
    // Tests - Calculate Command - Error Handling
    // ============================================================

    /// <summary>
    /// Verifies that exceptions during calculation are caught, result is cleared,
    /// and error status is displayed with the exception message.
    /// </summary>
    [Fact]
    public void CalculateCommand_Exception_SetsErrorStatus()
    {
        var vm = CreateVm(out var statusMock, out var estimatorMock, out _);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Throws(new InvalidOperationException("boom"));

        vm.CalculateCommand.Execute(null);

        Assert.Null(vm.Result);

        statusMock.Verify(s =>
            s.Set(AppStatusLevel.Error, "Calculation failed", "boom"),
            Times.Once);
    }

    // ============================================================
    // Tests - Clear Command
    // ============================================================

    /// <summary>
    /// Verifies that the Clear command resets all inputs to defaults, clears the result,
    /// and re-seeds text fields with default values.
    /// </summary>
    [Fact]
    public void ClearCommand_ResetsDefaults_AndResult()
    {
        var vm = CreateVm(out _, out var estimatorMock, out _);

        estimatorMock
            .Setup(e => e.Estimate(It.IsAny<CastIronInputs>()))
            .Returns(CreateEstimate());

        // Perform calculation to set result
        vm.CalculateCommand.Execute(null);
        Assert.NotNull(vm.Result);

        // Clear should reset everything
        vm.ClearCommand.Execute(null);

        Assert.Null(vm.Result);

        // Verify numeric values reset to defaults
        Assert.Equal(3.40, vm.Carbon, 3);
        Assert.Equal(12.0, vm.ThicknessValue, 3);
        Assert.Equal(1.0, vm.CoolingRateValue, 3);

        // Verify text fields were reseeded (not empty)
        Assert.False(string.IsNullOrWhiteSpace(vm.CarbonText));
        Assert.False(string.IsNullOrWhiteSpace(vm.ThicknessText));
    }

    // ============================================================
    // Tests - Chart Updates - Composition Plot
    // ============================================================

    /// <summary>
    /// Verifies that changing composition values updates the composition bar chart in real-time.
    /// </summary>
    [Fact]
    public void ChangingComposition_UpdatesPlotValues()
    {
        var vm = CreateVm(out _, out _, out _);

        vm.CarbonText = "4.0";

        var barSeries =
            vm.CompositionPlotModel.Series
                .OfType<BarSeries>()
                .Single();

        Assert.Equal(4.0, barSeries.Items[0].Value, 6);
    }

    // ============================================================
    // Tests - Theme Management
    // ============================================================

    /// <summary>
    /// Verifies that changing the theme rebuilds plot models with appropriate colors and styling.
    /// </summary>
    /// <remarks>
    /// Plot models are completely rebuilt (new instances) on theme changes to ensure
    /// all colors, axis styling, and visual elements properly reflect the new theme.
    /// </remarks>
    [Fact]
    public void ChangingTheme_RebuildsPlotModels()
    {
        var vm = CreateVm(out _, out _, out _);

        var before = vm.CompositionPlotModel;

        vm.IsDarkTheme = !vm.IsDarkTheme;

        Assert.NotSame(before, vm.CompositionPlotModel);
    }
}