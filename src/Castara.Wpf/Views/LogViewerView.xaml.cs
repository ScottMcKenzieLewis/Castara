using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Castara.Wpf.Views;

public partial class LogViewerView : UserControl
{
    public LogViewerView()
    {
        InitializeComponent();
    }

    private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is DataGrid dg)
            dg.SelectAll();
    }
}