using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Exceptions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Tests.Common;
using Castara.Wpf.ViewModels;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Castara.Wpf.Tests.ViewModels;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeExpectedDefaults()
    {
        var ctx = new TestContext();

        var vm = ctx.CreateShellViewModel();

        vm.IsDarkMode.Should().BeTrue();
        vm.UnitSystem.Should().Be(UnitSystem.Standard);
        vm.IsAmericanStandard.Should().BeFalse();

        vm.UnitSystemLeftText.Should().Be("Units");
        vm.UnitSystemRightText.Should().Be("Standard");

        vm.IsLoadingCastingProfiles.Should().BeFalse();
        vm.CastingProfilesLoadError.Should().BeNull();

        vm.CastingProfiles.Should().BeEmpty();
        vm.CastingProfileOptions.Should().NotBeNull();
        vm.CastingProfileOptions.Should().HaveCount(1);

        vm.SelectedCastingProfile.Should().BeNull();
        vm.SelectedCastingProfileDisplayName.Should().Be("Select a casting profile");
        vm.SelectedCastingProfileDescriptor.Should().Be(
            "Choose a casting profile to load defaults and estimation assumptions.");

        ctx.ThemeService.Verify(x => x.SetDark(true), Times.Once);
        ctx.ThemeAware.Verify(x => x.SetTheme(true), Times.Once);
        ctx.UnitAware.VerifySet(x => x.UnitSystem = UnitSystem.Standard, Times.Once);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile"),
            Times.Once);
    }

    [Fact]
    public void IsDarkMode_ShouldUpdateTheme_WhenChanged()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        ctx.ThemeService.Invocations.Clear();
        ctx.ThemeAware.Invocations.Clear();

        vm.IsDarkMode = false;

        vm.IsDarkMode.Should().BeFalse();

        ctx.ThemeService.Verify(x => x.SetDark(false), Times.Once);
        ctx.ThemeAware.Verify(x => x.SetTheme(false), Times.Once);
    }

    [Fact]
    public void IsDarkMode_ShouldNotInvokeThemeServices_WhenValueDoesNotChange()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        ctx.ThemeService.Invocations.Clear();
        ctx.ThemeAware.Invocations.Clear();

        vm.IsDarkMode = true;

        ctx.ThemeService.Verify(x => x.SetDark(It.IsAny<bool>()), Times.Never);
        ctx.ThemeAware.Verify(x => x.SetTheme(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void UnitSystem_ShouldUpdateUnitAware_WhenChanged()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        ctx.UnitAware.Invocations.Clear();

        vm.UnitSystem = UnitSystem.AmericanStandard;

        vm.UnitSystem.Should().Be(UnitSystem.AmericanStandard);
        vm.IsAmericanStandard.Should().BeTrue();
        vm.UnitSystemLeftText.Should().Be("Units");
        vm.UnitSystemRightText.Should().Be("American");

        ctx.UnitAware.VerifySet(x => x.UnitSystem = UnitSystem.AmericanStandard, Times.Once);
    }

    [Fact]
    public void IsAmericanStandard_ShouldMapToUnderlyingUnitSystem()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        vm.IsAmericanStandard = true;

        vm.UnitSystem.Should().Be(UnitSystem.AmericanStandard);
        vm.IsAmericanStandard.Should().BeTrue();

        vm.IsAmericanStandard = false;

        vm.UnitSystem.Should().Be(UnitSystem.Standard);
        vm.IsAmericanStandard.Should().BeFalse();
    }

    [Fact]
    public void SelectedCastingProfile_ShouldUpdateAwareAndStatus_WhenProfileChosen()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.CastingProfileAware.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        vm.SelectedCastingProfile = profile;

        vm.SelectedCastingProfile.Should().Be(profile);
        vm.SelectedCastingProfileDisplayName.Should().Be(profile.DisplayName);
        vm.SelectedCastingProfileDescriptor.Should().Be(
            $"{profile.IronType} • {profile.ProcessFamily}");

        ctx.CastingProfileAware.Verify(x => x.SetCastingProfile(profile), Times.Once);
        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Profile", profile.DisplayName),
            Times.Once);
    }

    [Fact]
    public void SelectedCastingProfile_ShouldResetStatus_WhenCleared()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        vm.SelectedCastingProfile = profile;

        ctx.CastingProfileAware.Invocations.Clear();
        ctx.Status.Invocations.Clear();

        vm.SelectedCastingProfile = null;

        vm.SelectedCastingProfile.Should().BeNull();
        vm.SelectedCastingProfileDisplayName.Should().Be("Select a casting profile");
        vm.SelectedCastingProfileDescriptor.Should().Be(
            "Choose a casting profile to load defaults and estimation assumptions.");

        ctx.CastingProfileAware.Verify(
            x => x.SetCastingProfile(It.IsAny<CastingProfileDefinition>()),
            Times.Never);

        ctx.Status.Verify(
            x => x.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile"),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldPopulateProfilesAndOptions_WhenRepositorySucceeds()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.CastingProfileRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([profile]);

        await vm.InitializeAsync();

        vm.IsLoadingCastingProfiles.Should().BeFalse();
        vm.CastingProfilesLoadError.Should().BeNull();

        vm.CastingProfiles.Should().HaveCount(1);
        vm.CastingProfiles[0].Should().Be(profile);

        vm.CastingProfileOptions.Should().HaveCount(2);
        vm.SelectedCastingProfile.Should().BeNull();
        vm.SelectedCastingProfileDisplayName.Should().Be("Select a casting profile");

        ctx.CastingProfileRepository.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldNotReload_WhenProfilesAlreadyExist()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();
        var profile = CastingProfileTestData.CreateValidProfile();

        ctx.CastingProfileRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([profile]);

        await vm.InitializeAsync();
        await vm.InitializeAsync();

        ctx.CastingProfileRepository.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetErrorStatus_WhenRepositoryThrowsDomainException()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        ctx.CastingProfileRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Domain blew up."));

        await vm.InitializeAsync();

        vm.IsLoadingCastingProfiles.Should().BeFalse();
        vm.CastingProfilesLoadError.Should().Be("Domain blew up.");
        vm.CastingProfiles.Should().BeEmpty();

        ctx.Status.Verify(
            x => x.Set(
                AppStatusLevel.Error,
                "Profile load failed",
                "Domain blew up."),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetGenericErrorStatus_WhenRepositoryThrowsUnexpectedException()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();

        ctx.CastingProfileRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        await vm.InitializeAsync();

        vm.IsLoadingCastingProfiles.Should().BeFalse();
        vm.CastingProfilesLoadError.Should().Be("Boom");
        vm.CastingProfiles.Should().BeEmpty();

        ctx.Status.Verify(
            x => x.Set(
                AppStatusLevel.Error,
                "Profile load failed",
                "Unexpected error occurred"),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldPassCancellationToken_ToRepository()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateShellViewModel();
        var cts = new CancellationTokenSource();

        ctx.CastingProfileRepository
            .Setup(x => x.GetAllAsync(cts.Token))
            .ReturnsAsync([]);

        await vm.InitializeAsync(cts.Token);

        ctx.CastingProfileRepository.Verify(
            x => x.GetAllAsync(cts.Token),
            Times.Once);
    }
}