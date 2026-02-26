using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;

using Moq;
using Xunit;

namespace Castara.Wpf.Tests;

public sealed class ShellViewModelTests
{
    private static void EnsureWpfApplication()
    {
        if (System.Windows.Application.Current is null)
            _ = new System.Windows.Application();
    }

    private static Mock<IStatusService> CreateStatusServiceMock(StatusState initial)
    {
        var status = new Mock<IStatusService>(MockBehavior.Strict);

        // Shell reads Current for StatusLeftText/StatusRightText/StatusBrush
        StatusState current = initial;
        status.SetupGet(s => s.Current).Returns(() => current);

        // Shell subscribes to PropertyChanged
        status.SetupAdd(s => s.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());

        // Shell sets initial status in ctor
        status.Setup(s => s.Set(It.IsAny<AppStatusLevel>(), It.IsAny<string>(), It.IsAny<string>()));

        return status;
    }

    [StaFact]
    public void Ctor_Sets_CurrentViewModel_Initializes_Theme_And_Status_And_Defaults_DarkMode()
    {
        EnsureWpfApplication();

        var theme = new Mock<IThemeService>(MockBehavior.Strict);
        var calcVm = new Mock<IThemeAware>(MockBehavior.Strict);

        var status = CreateStatusServiceMock(
            new StatusState(AppStatusLevel.Ok, "—", "—"));

        // ctor triggers IsDarkMode = true -> calls both theme systems once
        theme.Setup(t => t.SetDark(true));
        calcVm.Setup(v => v.SetTheme(true));

        // ctor sets initial status
        status.Setup(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        var vm = new ShellViewModel(theme.Object, status.Object, calcVm.Object);

        Assert.True(vm.IsDarkMode);
        Assert.Same(calcVm.Object, vm.CurrentViewModel);

        theme.Verify(t => t.SetDark(true), Times.Once);
        calcVm.Verify(v => v.SetTheme(true), Times.Once);
        status.Verify(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"), Times.Once);

        theme.VerifyNoOtherCalls();
        calcVm.VerifyNoOtherCalls();
    }

    [StaFact]
    public void IsDarkMode_When_Changed_Calls_Both_ThemeSystems_And_Raises_PropertyChanged()
    {
        EnsureWpfApplication();

        var theme = new Mock<IThemeService>(MockBehavior.Strict);
        var calcVm = new Mock<IThemeAware>(MockBehavior.Strict);

        var status = CreateStatusServiceMock(
            new StatusState(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        // ctor
        theme.Setup(t => t.SetDark(true));
        calcVm.Setup(v => v.SetTheme(true));
        status.Setup(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        var vm = new ShellViewModel(theme.Object, status.Object, calcVm.Object);

        // toggle to light
        theme.Setup(t => t.SetDark(false));
        calcVm.Setup(v => v.SetTheme(false));

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.IsDarkMode = false;

        Assert.False(vm.IsDarkMode);
        Assert.Contains(nameof(ShellViewModel.IsDarkMode), changed);

        theme.Verify(t => t.SetDark(false), Times.Once);
        calcVm.Verify(v => v.SetTheme(false), Times.Once);
    }

    [StaFact]
    public void IsDarkMode_Set_To_Same_Value_Does_Not_Call_Services_Again()
    {
        EnsureWpfApplication();

        var theme = new Mock<IThemeService>(MockBehavior.Strict);
        var calcVm = new Mock<IThemeAware>(MockBehavior.Strict);

        var status = CreateStatusServiceMock(
            new StatusState(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        // ctor
        theme.Setup(t => t.SetDark(true));
        calcVm.Setup(v => v.SetTheme(true));
        status.Setup(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        var vm = new ShellViewModel(theme.Object, status.Object, calcVm.Object);

        // setting same value should short-circuit
        vm.IsDarkMode = true;

        theme.Verify(t => t.SetDark(true), Times.Once);
        calcVm.Verify(v => v.SetTheme(true), Times.Once);
    }

    [StaFact]
    public void When_StatusService_Current_Changes_VM_Raises_StatusLeftRightBrush_Notifications()
    {
        EnsureWpfApplication();

        var theme = new Mock<IThemeService>(MockBehavior.Strict);
        var calcVm = new Mock<IThemeAware>(MockBehavior.Strict);

        var status = new Mock<IStatusService>(MockBehavior.Strict);

        // capture the handler Shell registers
        PropertyChangedEventHandler? captured = null;

        var current = new StatusState(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
        status.SetupGet(s => s.Current).Returns(() => current);

        status.SetupAdd(s => s.PropertyChanged += It.IsAny<PropertyChangedEventHandler>())
              .Callback<PropertyChangedEventHandler>(h => captured += h);

        // ctor calls
        status.Setup(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));
        theme.Setup(t => t.SetDark(true));
        calcVm.Setup(v => v.SetTheme(true));

        var vm = new ShellViewModel(theme.Object, status.Object, calcVm.Object);

        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        Assert.NotNull(captured);

        // simulate status service raising Current changed
        captured!(status.Object, new PropertyChangedEventArgs(nameof(IStatusService.Current)));

        Assert.Contains(nameof(ShellViewModel.StatusLeftText), changes);
        Assert.Contains(nameof(ShellViewModel.StatusRightText), changes);
        Assert.Contains(nameof(ShellViewModel.StatusBrush), changes);
    }

    [StaFact]
    public void StatusBrush_Maps_StatusLevel_To_Expected_Color()
    {
        EnsureWpfApplication();

        var theme = new Mock<IThemeService>(MockBehavior.Strict);
        var calcVm = new Mock<IThemeAware>(MockBehavior.Strict);

        var status = CreateStatusServiceMock(
            new StatusState(AppStatusLevel.Warning, "X", "Y"));

        // ctor
        theme.Setup(t => t.SetDark(true));
        calcVm.Setup(v => v.SetTheme(true));
        status.Setup(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"));

        var vm = new ShellViewModel(theme.Object, status.Object, calcVm.Object);

        var brush = Assert.IsType<SolidColorBrush>(vm.StatusBrush);

        var expected =
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCC00");

        Assert.Equal(expected, brush.Color);
    }
}