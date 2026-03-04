using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Castara.Wpf.Infrastructure.Components;

public static class DataGridSelectedItemsBehavior
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(DataGridSelectedItemsBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    public static void SetSelectedItems(DependencyObject element, IList value)
        => element.SetValue(SelectedItemsProperty, value);

    public static IList GetSelectedItems(DependencyObject element)
        => (IList)element.GetValue(SelectedItemsProperty);

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;

        grid.SelectionChanged -= Grid_SelectionChanged;
        grid.SelectionChanged += Grid_SelectionChanged;

        // Push initial selection into VM if needed
        if (e.NewValue is IList list)
        {
            // VM list reference will be updated via SelectionChanged anyway
        }
    }

    private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        var boundList = GetSelectedItems(grid);
        if (boundList is null) return;

        // Replace reference with the live SelectedItems collection
        if (!ReferenceEquals(boundList, grid.SelectedItems))
        {
            SetSelectedItems(grid, grid.SelectedItems);
        }
    }
}