using Castara.Wpf.Diagnostics.CrashReport;
using FluentAssertions;
using System.ComponentModel;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

public sealed class CrashReportDialogViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_WhenArgumentsAreValid()
    {
        // Arrange
        const string reportJson = "{ \"error\": \"boom\" }";
        const string reportId = "abc123";

        // Act
        var sut = new CrashReportDialogViewModel(
            reportJson,
            reportId,
            isCrashReportUploadEnabled: true);

        // Assert
        sut.ReportJson.Should().Be(reportJson);
        sut.ReportId.Should().Be(reportId);
        sut.IsCrashReportUploadEnabled.Should().BeTrue();

        sut.Title.Should().Be("Castara encountered an unexpected error");
        sut.Message.Should().Contain("Castara ran into an unexpected problem");

        sut.SendReport.Should().BeFalse();
        sut.SaveLocally.Should().BeTrue();

        sut.LocalSaveDirectory.Should().Contain("Castara");
        sut.LocalSaveDirectory.Should().Contain("CrashReports");

        sut.LocalSaveFileName.Should().Be("abc123.json");
        sut.LocalSavePath.Should().EndWith(
            System.IO.Path.Combine("CrashReports", "abc123.json"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenReportJsonIsNull()
    {
        // Act
        var act = () => new CrashReportDialogViewModel(
            null!,
            "abc123",
            true);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportJson");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenReportIdIsNull()
    {
        // Act
        var act = () => new CrashReportDialogViewModel(
            "{ }",
            null!,
            true);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportId");
    }

    [Fact]
    public void HasPrimaryAction_ShouldBeTrue_ByDefault()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        sut.HasPrimaryAction.Should().BeTrue();
    }

    [Fact]
    public void PrimaryActionText_ShouldBeSaveAndClose_WhenSaveOnly()
    {
        var sut = CreateSut();

        sut.SaveLocally = true;
        sut.SendReport = false;

        sut.PrimaryActionText.Should().Be("Save and Close");
    }

    [Fact]
    public void PrimaryActionText_ShouldBeSendAndClose_WhenSendOnly()
    {
        var sut = CreateSut();

        sut.SaveLocally = false;
        sut.SendReport = true;

        sut.PrimaryActionText.Should().Be("Send and Close");
    }

    [Fact]
    public void PrimaryActionText_ShouldBeSaveSendAndClose_WhenBothSelected()
    {
        var sut = CreateSut();

        sut.SaveLocally = true;
        sut.SendReport = true;

        sut.PrimaryActionText.Should().Be("Save, Send and Close");
    }

    [Fact]
    public void PrimaryActionText_ShouldBeEmpty_WhenNoneSelected()
    {
        var sut = CreateSut();

        sut.SaveLocally = false;
        sut.SendReport = false;

        sut.HasPrimaryAction.Should().BeFalse();
        sut.PrimaryActionText.Should().BeEmpty();
    }

    [Fact]
    public void SaveLocally_ShouldRaisePropertyChanged_ForDependentProperties()
    {
        var sut = CreateSut();
        var changes = new List<string>();

        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changes.Add(e.PropertyName);
        };

        sut.SaveLocally = false;

        changes.Should().Contain(nameof(CrashReportDialogViewModel.SaveLocally));
        changes.Should().Contain(nameof(CrashReportDialogViewModel.HasPrimaryAction));
        changes.Should().Contain(nameof(CrashReportDialogViewModel.PrimaryActionText));
    }

    [Fact]
    public void SendReport_ShouldRaisePropertyChanged_ForDependentProperties()
    {
        var sut = CreateSut();
        var changes = new List<string>();

        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changes.Add(e.PropertyName);
        };

        sut.SendReport = true;

        changes.Should().Contain(nameof(CrashReportDialogViewModel.SendReport));
        changes.Should().Contain(nameof(CrashReportDialogViewModel.HasPrimaryAction));
        changes.Should().Contain(nameof(CrashReportDialogViewModel.PrimaryActionText));
    }

    [Fact]
    public void ContinueCommand_ShouldRaiseCloseRequested_WithTrue()
    {
        var sut = CreateSut();

        bool? accepted = null;
        sut.CloseRequested += (_, value) => accepted = value;

        sut.ContinueCommand.Execute(null);

        accepted.Should().BeTrue();
    }

    [Fact]
    public void CloseCommand_ShouldRaiseCloseRequested_WithFalse()
    {
        var sut = CreateSut();

        bool? accepted = null;
        sut.CloseRequested += (_, value) => accepted = value;

        sut.CloseCommand.Execute(null);

        accepted.Should().BeFalse();
    }

    [Fact]
    public void CopyCommand_ShouldRaiseCopyRequested_WithReportJson()
    {
        const string json = "{ \"error\": \"boom\" }";
        var sut = new CrashReportDialogViewModel(json, "abc123", true);

        string? copied = null;
        sut.CopyRequested += (_, value) => copied = value;

        sut.CopyCommand.Execute(null);

        copied.Should().Be(json);
    }

    [Fact]
    public void CopySavePathCommand_ShouldRaiseCopyRequested_WithPath()
    {
        var sut = CreateSut();

        string? copied = null;
        sut.CopyRequested += (_, value) => copied = value;

        sut.CopySavePathCommand.Execute(null);

        copied.Should().Be(sut.LocalSavePath);
    }

    [Fact]
    public void LocalSavePath_ShouldUseReportId()
    {
        var sut = new CrashReportDialogViewModel("{ }", "report-42", true);

        sut.LocalSaveFileName.Should().Be("report-42.json");
        sut.LocalSavePath.Should().EndWith(
            System.IO.Path.Combine("CrashReports", "report-42.json"));
    }

    private static CrashReportDialogViewModel CreateSut()
        => new("{ \"error\": \"boom\" }", "abc123", true);
}