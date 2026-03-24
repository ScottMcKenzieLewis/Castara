// <copyright file="CalculationsViewModelTests.cs" company="Castara">
// Copyright (c) Castara. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Tests.Common;
using Castara.Wpf.ViewModels;
using FluentAssertions.Events;
using Moq;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Contains unit tests for the <see cref="CalculationsViewModel"/> class.
/// Tests cover initialization, calculation logic, validation, unit system changes,
/// casting profile management, and UI state behavior.
/// </summary>
public sealed class CalculationsViewModelTests
{
    /// <summary>
    /// Verifies that the CalculationsViewModel constructor initializes all properties
    /// to their expected default values, including theme, unit system, composition values,
    /// and status messages.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeExpectedDefaults()
    {
        var ctx = new TestContext();

        var vm = ctx.CreateCalculationsViewModel();

        vm.IsDarkTheme.Should().BeTrue();
        vm.UnitSystem.Should().Be(UnitSystem.Standard);

        vm.CarbonText.Should().Be("3.4");
        vm.SiliconText.Should().Be("2.1");
        vm.ManganeseText.Should().Be("0.55");
        vm.PhosphorusText.Should().Be("0.05");
        vm.SulfurText.Should().Be("0.02");

        vm.ThicknessText.Should().Be("12");
        vm.CoolingRateText.Should().Be("1");

        vm.Result.Should().BeNull();
        vm.CarbonEquivalentText.Should().Be("—");
        vm.GraphitizationScoreText.Should().Be("—");
        vm.HardnessText.Should().Be("—");
        vm.SelectedCastingProfile.Should().BeNull();
        vm.SelectedCastingProfileDisplayName.Should().Be("Select a casting profile");
        vm.CanCalculateDisplay.Should().BeFalse();

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that executing the Calculate command without selecting a casting profile
    /// displays a warning message and does not invoke the estimator.
    /// </summary>
    [Fact]
    public void CalculateCommand_ShouldWarn_WhenNoProfileIsSelected()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();

        vm.CalculateCommand.Execute(null);

        vm.Result.Should().BeNull();

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), It.IsAny<CastingProfileDefinition>()),
            Times.Never);

        ctx.Status.Verify(
            x => x.Set(
                AppStatusLevel.Warning,
                "No profile selected",
                "A casting profile must be selected before calculation."),
            Times.Once);
    }

    /// <summary>
    /// Verifies that setting a casting profile updates the SelectedCastingProfile property,
    /// updates the display name, and enables the calculation command.
    /// </summary>
    [Fact]
    public void SetCastingProfile_ShouldUpdateSelectedProfile_AndEnableCalculation()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);

        vm.SelectedCastingProfile.Should().Be(profile);
        vm.SelectedCastingProfileDisplayName.Should().Be(profile.DisplayName);
        vm.CanCalculateDisplay.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that setting a casting profile automatically triggers a calculation when inputs are valid,
    /// and that the result is properly populated when the estimator returns a valid estimate.
    /// </summary>
    [Fact]
    public void SetCastingProfile_ShouldAutoCalculate_WhenInputsAreValid_AndEstimatorReturnsEstimate()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 4.210,
            GraphitizationScore: 0.820,
            EstimatedHardness: new HardnessRange(180, 220),
            CoolingFactor: 0.910,
            ThicknessFactor: 0.770,
            Flags: new List<RiskFlag>());

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns(estimate);

        vm.SetCastingProfile(profile);

        vm.Result.Should().NotBeNull();
        vm.CarbonEquivalentText.Should().Be("4.210");
        vm.GraphitizationScoreText.Should().Be("0.820");
        vm.HardnessText.Should().Be("180-220 HB");
        vm.CoolingFactorText.Should().Be("0.910");
        vm.ThicknessFactorText.Should().Be("0.770");
        vm.Flags.Should().BeEmpty();

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), profile),
            Times.Once);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Calculated", "OK"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that when setting a casting profile triggers auto-calculation but the estimator returns null,
    /// the result remains null and an appropriate warning status is displayed.
    /// </summary>
    [Fact]
    public void SetCastingProfile_ShouldAutoCalculate_WhenInputsAreValid_AndEstimatorReturnsNull()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);

        vm.Result.Should().BeNull();
        vm.CarbonEquivalentText.Should().Be("—");
        vm.GraphitizationScoreText.Should().Be("—");
        vm.HardnessText.Should().Be("—");

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), profile),
            Times.Once);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Warning, "No result", "Estimator returned no result."),
            Times.Once);
    }

    /// <summary>
    /// Verifies that executing the Calculate command with invalid input values
    /// displays a warning message, does not invoke the estimator, and disables the calculation capability.
    /// </summary>
    [Fact]
    public void CalculateCommand_ShouldWarn_WhenInputsAreInvalid()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);

        ctx.Estimator.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        vm.CarbonText = "999";

        vm.IsValid.Should().BeFalse();
        vm.CanCalculateDisplay.Should().BeFalse();

        vm.CalculateCommand.Execute(null);

        vm.Result.Should().BeNull();

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), It.IsAny<CastingProfileDefinition>()),
            Times.Never);

        ctx.Status.Verify(
            x => x.Set(
                AppStatusLevel.Warning,
                "Check inputs",
                "One or more fields are invalid."),
            Times.Once);
    }

    /// <summary>
    /// Verifies that executing the Calculate command with valid inputs and a selected profile
    /// invokes the estimator and properly populates all result properties when an estimate is returned.
    /// </summary>
    [Fact]
    public void CalculateCommand_ShouldPopulateResult_WhenEstimatorReturnsEstimate()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 4.210,
            GraphitizationScore: 0.820,
            EstimatedHardness: new HardnessRange(180, 220),
            CoolingFactor: 0.910,
            ThicknessFactor: 0.770,
            Flags: []);

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns(estimate);

        vm.SetCastingProfile(profile);

        ctx.Estimator.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        vm.CalculateCommand.Execute(null);

        vm.Result.Should().NotBeNull();
        vm.CarbonEquivalentText.Should().Be("4.210");
        vm.GraphitizationScoreText.Should().Be("0.820");
        vm.HardnessText.Should().Be("180-220 HB");
        vm.CoolingFactorText.Should().Be("0.910");
        vm.ThicknessFactorText.Should().Be("0.770");
        vm.Flags.Should().BeEmpty();

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), profile),
            Times.Once);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Calculated", "OK"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that when the Calculate command is executed and the estimator returns null,
    /// the result is cleared and an appropriate warning status is displayed.
    /// </summary>
    [Fact]
    public void CalculateCommand_ShouldClearResult_WhenEstimatorReturnsNull()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);

        ctx.Estimator.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        vm.CalculateCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.CarbonEquivalentText.Should().Be("—");
        vm.GraphitizationScoreText.Should().Be("—");
        vm.HardnessText.Should().Be("—");

        ctx.Estimator.Verify(
            x => x.Estimate(It.IsAny<CastIronInputs>(), profile),
            Times.Once);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Warning, "No result", "Estimator returned no result."),
            Times.Once);
    }

    /// <summary>
    /// Verifies that when the estimator throws an exception during calculation,
    /// the exception is handled gracefully, the result is cleared, and an error status is displayed.
    /// </summary>
    [Fact]
    public void CalculateCommand_ShouldSetErrorStatus_WhenEstimatorThrows()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Throws(new InvalidOperationException("Boom"));

        vm.SetCastingProfile(profile);

        ctx.Estimator.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        Action act = () => vm.CalculateCommand.Execute(null);

        act.Should().NotThrow();
        vm.Result.Should().BeNull();

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Error, "Calculation failed", "Boom"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that executing the Clear command resets all input fields to their default values,
    /// clears the calculation result, and displays an appropriate ready status when a profile is selected.
    /// </summary>
    [Fact]
    public void ClearCommand_ShouldResetInputs_AndClearResult_WhenProfileIsSelected()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        var estimate = new CastIronEstimate(
            CarbonEquivalent: 4.210,
            GraphitizationScore: 0.820,
            EstimatedHardness: new HardnessRange(180, 220),
            CoolingFactor: 0.910,
            ThicknessFactor: 0.770,
            Flags: []);

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns(estimate);

        vm.SetCastingProfile(profile);

        ctx.Status.Invocations.Clear();

        vm.CarbonText = "3.8";
        vm.ThicknessText = "20";
        vm.ClearCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.CarbonText.Should().Be("3.4");
        vm.SiliconText.Should().Be("2.1");
        vm.ManganeseText.Should().Be("0.55");
        vm.PhosphorusText.Should().Be("0.05");
        vm.SulfurText.Should().Be("0.02");
        vm.ThicknessText.Should().Be("12");
        vm.CoolingRateText.Should().Be("1");

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that executing the Clear command when no casting profile is selected
    /// resets all input fields to defaults and displays a message prompting profile selection.
    /// </summary>
    [Fact]
    public void ClearCommand_ShouldResetInputs_AndSetSelectProfileStatus_WhenNoProfileIsSelected()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();

        vm.CarbonText = "3.8";
        vm.ThicknessText = "20";

        ctx.Status.Invocations.Clear();

        vm.ClearCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.CarbonText.Should().Be("3.4");
        vm.ThicknessText.Should().Be("12");
        vm.CoolingRateText.Should().Be("1");

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that changing the unit system to American Standard updates all labels,
    /// suffixes, and converts displayed values to the appropriate units (inches, °F/s).
    /// </summary>
    [Fact]
    public void UnitSystem_ShouldUpdateLabelsAndDisplayValues_WhenChangedToAmericanStandard()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();

        vm.UnitSystem = UnitSystem.AmericanStandard;

        vm.ThicknessLabel.Should().Be("Thickness (in)");
        vm.CoolingRateLabel.Should().Be("Cooling Rate (°F/s)");
        vm.CoolingRateUnitSuffix.Should().Be("°F/s");
        vm.ThicknessText.Should().StartWith("0.472");
        vm.CoolingRateText.Should().Be("1.8");
    }

    /// <summary>
    /// Verifies that changing the carbon text input raises the appropriate PropertyChanged events
    /// for CarbonText, IsValid, and CanCalculateDisplay properties.
    /// </summary>
    [Fact]
    public void ChangingCarbonText_ShouldRaiseExpectedPropertyChangedEvents()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();

        using var monitor = vm.Monitor();

        vm.CarbonText = "3.7";

        monitor.Should().RaisePropertyChangeFor(x => x.CarbonText);
        monitor.Should().RaisePropertyChangeFor(x => x.IsValid);
        monitor.Should().RaisePropertyChangeFor(x => x.CanCalculateDisplay);
    }

    /// <summary>
    /// Verifies that the IDataErrorInfo implementation returns an appropriate validation error message
    /// when an input value falls outside the valid range defined by the selected casting profile.
    /// </summary>
    [Fact]
    public void DataErrorInfo_ShouldReturnValidationMessage_WhenValueFallsOutsideProfileRange()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateNarrowCarbonProfile();

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);
        vm.CarbonText = "3.9";

        var error = ((IDataErrorInfo)vm)[nameof(CalculationsViewModel.CarbonText)];

        error.Should().Contain("Carbon must be between 3.2 and 3.6");
        error.Should().Contain(profile.DisplayName);
    }

    /// <summary>
    /// Verifies that the SetTheme method properly updates the IsDarkTheme property.
    /// </summary>
    [Fact]
    public void SetTheme_ShouldToggleIsDarkTheme()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();

        vm.SetTheme(false);

        vm.IsDarkTheme.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that setting a casting profile applies the profile's default section thickness
    /// to the thickness input field.
    /// </summary>
    [Fact]
    public void SetCastingProfile_ShouldApplyDefaultSectionThicknessFromProfile()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateCalculationsViewModel();
        var profile = CastingProfileTestData.CreateValidProfile(defaultSectionThicknessMm: 25.0);

        ctx.Estimator
            .Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), profile))
            .Returns((CastIronEstimate)null!);

        vm.SetCastingProfile(profile);

        vm.ThicknessValue.Should().BeApproximately(25.0, 0.001);
        vm.ThicknessText.Should().Be("25");
    }
}