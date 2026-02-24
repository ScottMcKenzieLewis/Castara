using System.Windows.Controls;
using Castara.Wpf.ViewModels;

namespace Castara.Wpf.Views;

public partial class CalculationsView : UserControl
{
    public CalculationsView(CalculationsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}