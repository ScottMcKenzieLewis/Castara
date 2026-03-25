using Castara.Wpf.Diagnostics;
using Castara.Wpf.Diagnostics.CrashReport;
using FluentAssertions;

namespace Castara.Wpf.Tests.Diagnostics.CrashReport;

/// <summary>
/// Contains unit tests for <see cref="ApplicationStateSnapshotService"/>.
/// </summary>
public sealed class ApplicationStateSnapshotServiceTests
{
    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.SetValue"/> stores a value 
    /// and it can be retrieved via a snapshot.
    /// </summary>
    [Fact]
    public void SetValue_ShouldStoreValue()
    {
        var sut = new ApplicationStateSnapshotService();

        sut.SetValue(ApplicationStateKeys.Theme, "Light");

        var snapshot = sut.GetSnapshot();

        snapshot.Get(ApplicationStateKeys.Theme).Should().Be("Light");
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.SetValue"/> trims whitespace 
    /// from both keys and values before storing them.
    /// </summary>
    [Fact]
    public void SetValue_ShouldTrimKeyAndValue()
    {
        var sut = new ApplicationStateSnapshotService();

        sut.SetValue("  Theme  ", "  Dark  ");

        var snapshot = sut.GetSnapshot();

        snapshot.Get("Theme").Should().Be("Dark");
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.SetValue"/> removes an entry 
    /// when the value is set to <see langword="null"/>.
    /// </summary>
    [Fact]
    public void SetValue_ShouldRemoveEntry_WhenValueIsNull()
    {
        var sut = new ApplicationStateSnapshotService();
        sut.SetValue(ApplicationStateKeys.Theme, "Light");

        sut.SetValue(ApplicationStateKeys.Theme, null);

        var snapshot = sut.GetSnapshot();

        snapshot.Get(ApplicationStateKeys.Theme).Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.RemoveValue"/> removes 
    /// an existing entry from the state.
    /// </summary>
    [Fact]
    public void RemoveValue_ShouldRemoveEntry()
    {
        var sut = new ApplicationStateSnapshotService();
        sut.SetValue(ApplicationStateKeys.Theme, "Light");

        sut.RemoveValue(ApplicationStateKeys.Theme);

        var snapshot = sut.GetSnapshot();

        snapshot.Get(ApplicationStateKeys.Theme).Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.Clear"/> removes all entries 
    /// from the application state.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        var sut = new ApplicationStateSnapshotService();
        sut.SetValue(ApplicationStateKeys.Theme, "Light");
        sut.SetValue(ApplicationStateKeys.ActiveView, "CalculationsViewModel");

        sut.Clear();

        var snapshot = sut.GetSnapshot();

        snapshot.Values.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationStateSnapshotService.GetSnapshot"/> returns 
    /// an independent copy of the state that doesn't affect subsequent snapshots when mutated.
    /// </summary>
    [Fact]
    public void GetSnapshot_ShouldReturnCopy()
    {
        var sut = new ApplicationStateSnapshotService();
        sut.SetValue(ApplicationStateKeys.Theme, "Light");

        var snapshot = sut.GetSnapshot();
        var copy = snapshot.Values as Dictionary<string, string>;

        copy.Should().NotBeNull();
        copy![ApplicationStateKeys.Theme] = "Mutated";

        var secondSnapshot = sut.GetSnapshot();

        secondSnapshot.Get(ApplicationStateKeys.Theme).Should().Be("Light");
    }
}