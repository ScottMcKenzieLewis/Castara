using System.Windows.Controls;
using Castara.Wpf.ViewModels;

namespace Castara.Wpf.Views;

/// <summary>
/// Provides the code-behind for the calculations view, which displays the cast iron
/// composition input form, estimation results, visualizations, and risk flags.
/// </summary>
/// <remarks>
/// <para>
/// This view follows the Model-View-ViewModel (MVVM) pattern, with the view model
/// (<see cref="CalculationsViewModel"/>) containing all business logic and state management.
/// The view is responsible only for UI composition and data binding.
/// </para>
/// <para>
/// The view displays:
/// <list type="bullet">
///   <item><description>Input form for chemical composition (C, Si, Mn, P, S)</description></item>
///   <item><description>Section parameters (thickness, cooling rate)</description></item>
///   <item><description>Key performance indicators (Carbon Eq., Graphitization, Hardness)</description></item>
///   <item><description>Model inputs derived from user inputs</description></item>
///   <item><description>Composition bar chart visualization</description></item>
///   <item><description>Graphitization and hardness gauge displays</description></item>
///   <item><description>Risk flags with severity indicators</description></item>
/// </list>
/// </para>
/// <para>
/// The view uses Material Design in XAML components for consistent styling and
/// OxyPlot for chart rendering.
/// </para>
/// </remarks>
public partial class CalculationsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsView"/> class with the specified view model.
    /// </summary>
    /// <param name="vm">
    /// The calculations view model that manages the view's state, commands, and business logic.
    /// </param>
    /// <remarks>
    /// The view model is injected via dependency injection and set as the view's DataContext,
    /// enabling data binding between the XAML and the view model's properties and commands.
    /// This constructor-based injection ensures the view always has a valid view model instance.
    /// </remarks>
    public CalculationsView()
    {
        InitializeComponent();
    }
}