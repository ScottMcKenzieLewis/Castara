using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The view model for the calculations view, managing cast iron composition inputs,
/// estimation calculations, and result visualizations.
/// </summary>
/// <remarks>
/// <para>
/// This view model orchestrates the entire cast iron analysis workflow:
/// <list type="bullet">
///   <item><description>Collecting and validating chemical composition inputs (C, Si, Mn, P, S)</description></item>
///   <item><description>Collecting and validating section parameters (thickness, cooling rate)</description></item>
///   <item><description>Executing estimation calculations via the domain service</description></item>
///   <item><description>Displaying results (carbon equivalent, graphitization, hardness, risk flags)</description></item>
///   <item><description>Visualizing data through OxyPlot charts (composition bars, gauges)</description></item>
///   <item><description>Managing theme switching for consistent dark/light mode appearance</description></item>
/// </list>
/// </para>
/// <para>
/// The view model follows the MVVM pattern with full support for data binding and
/// property change notification. It integrates with the application's status service
/// for user feedback and the domain estimation service for calculations.
/// </para>
/// <para>
/// Chart theming is handled explicitly since OxyPlot does not automatically bind to
/// WPF theme resources. The <see cref="SetTheme"/> method should be called by the
/// shell view model when the application theme changes.
/// </para>
/// </remarks>
public sealed class CalculationsViewModel : INotifyPropertyChanged, IThemeAware
{
    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;

    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    // -----------------------------
    // Input ranges (single source of truth)
    // -----------------------------

    /// <summary>Minimum allowable carbon percentage (wt%).</summary>
    private const double CarbonMin = 0.0;
    /// <summary>Maximum allowable carbon percentage (wt%).</summary>
    private const double CarbonMax = 5.0;

    /// <summary>Minimum allowable silicon percentage (wt%).</summary>
    private const double SiliconMin = 0.0;
    /// <summary>Maximum allowable silicon percentage (wt%).</summary>
    private const double SiliconMax = 5.0;

    /// <summary>Minimum allowable manganese percentage (wt%).</summary>
    private const double ManganeseMin = 0.0;
    /// <summary>Maximum allowable manganese percentage (wt%).</summary>
    private const double ManganeseMax = 3.0;

    /// <summary>Minimum allowable phosphorus percentage (wt%).</summary>
    private const double PhosphorusMin = 0.0;
    /// <summary>Maximum allowable phosphorus percentage (wt%).</summary>
    private const double PhosphorusMax = 1.0;

    /// <summary>Minimum allowable sulfur percentage (wt%).</summary>
    private const double SulfurMin = 0.0;
    /// <summary>Maximum allowable sulfur percentage (wt%).</summary>
    private const double SulfurMax = 1.0;

    /// <summary>Minimum allowable section thickness in millimeters (must be greater than zero).</summary>
    private const double ThicknessMinMm = 0.0001;

    /// <summary>Minimum allowable cooling rate in °C/s (must be greater than zero).</summary>
    private const double CoolingRateMinCPerSec = 0.0001;

    /// <summary>Typical minimum cooling rate for guidance (°C/s).</summary>
    private const double CoolingRateTypicalMin = 0.05;
    /// <summary>Typical maximum cooling rate for guidance (°C/s).</summary>
    private const double CoolingRateTypicalMax = 20.0;

    /// <summary>
    /// Gets the formatted cooling factor text for display.
    /// </summary>
    /// <value>
    /// The cooling factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string CoolingFactorText => Result is null
        ? "—"
        : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted thickness factor text for display.
    /// </summary>
    /// <value>
    /// The thickness factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string ThicknessFactorText => Result is null
        ? "—"
        : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the tooltip text for the carbon input field.
    /// </summary>
    public string CarbonTooltip =>
        $"Carbon (C), wt%.\nValid range: {CarbonMin:0.##} – {CarbonMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the silicon input field.
    /// </summary>
    public string SiliconTooltip =>
        $"Silicon (Si), wt%.\nValid range: {SiliconMin:0.##} – {SiliconMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the manganese input field.
    /// </summary>
    public string ManganeseTooltip =>
        $"Manganese (Mn), wt%.\nValid range: {ManganeseMin:0.##} – {ManganeseMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the phosphorus input field.
    /// </summary>
    public string PhosphorusTooltip =>
        $"Phosphorus (P), wt%.\nValid range: {PhosphorusMin:0.##} – {PhosphorusMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the sulfur input field.
    /// </summary>
    public string SulfurTooltip =>
        $"Sulfur (S), wt%.\nValid range: {SulfurMin:0.##} – {SulfurMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the thickness input field.
    /// </summary>
    public string ThicknessTooltip =>
        $"Section thickness in millimeters.\nValid range: > 0 mm.";

    /// <summary>
    /// Gets the tooltip text for the cooling rate input field.
    /// </summary>
    public string CoolingRateTooltip =>
        $"Cooling rate in °C/s (continuous).\nValid range: > 0 °C/s.\nTypical casting guidance: {CoolingRateTypicalMin:0.##} – {CoolingRateTypicalMax:0.##} °C/s.";

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsViewModel"/> class.
    /// </summary>
    /// <param name="status">The status service for displaying application status messages.</param>
    /// <param name="estimator">The cast iron estimator service for performing calculations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="status"/> or <paramref name="estimator"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor performs the following initialization:
    /// <list type="number">
    ///   <item><description>Wires up commands (Calculate, Clear)</description></item>
    ///   <item><description>Initializes theme to dark mode (updated by ShellViewModel)</description></item>
    ///   <item><description>Builds initial plot models and series for all charts</description></item>
    ///   <item><description>Sets demo default values for composition and section parameters</description></item>
    ///   <item><description>Seeds charts with default data before first calculation</description></item>
    ///   <item><description>Sets initial status to "Ready"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default composition values represent a typical Class 30 gray iron.
    /// </para>
    /// </remarks>
    public CalculationsViewModel(
        IStatusService status,
        ICastIronEstimator estimator)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));

        CalculateCommand = new RelayCommand(Calculate);
        ClearCommand = new RelayCommand(Clear);

        // Default to dark; ShellViewModel should call SetTheme(...) to keep plots in sync
        _isDarkTheme = true;

        // Build plot models + series FIRST (so UpdateCompositionPlot can safely run from setters)
        RebuildPlotsForTheme();

        // Demo defaults (typical Class 30 gray iron composition)
        _carbon = 3.40;
        _silicon = 2.10;
        _manganese = 0.55;
        _phosphorus = 0.05;
        _sulfur = 0.02;

        // Demo defaults (section parameters)
        _thicknessMm = 12.0;
        _coolingRateCPerSec = 1.0;

        // Ensure UI sees defaults immediately
        OnPropertyChanged(nameof(Carbon));
        OnPropertyChanged(nameof(Silicon));
        OnPropertyChanged(nameof(Manganese));
        OnPropertyChanged(nameof(Phosphorus));
        OnPropertyChanged(nameof(Sulfur));
        OnPropertyChanged(nameof(ThicknessMm));
        OnPropertyChanged(nameof(CoolingRateCPerSec));
        OnPropertyChanged(nameof(CoolingFactorText));
        OnPropertyChanged(nameof(ThicknessFactorText));

        // Seed charts with defaults (before first calc)
        UpdateCompositionPlot();
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // -----------------------------
    // Theme (for PlotModels)
    // -----------------------------

    private bool _isDarkTheme;

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled for chart visualizations.
    /// </summary>
    /// <value>
    /// <c>true</c> if dark theme is enabled; otherwise, <c>false</c> for light theme.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is used exclusively for OxyPlot chart theming, as OxyPlot does not
    /// automatically bind to WPF theme resources. When the theme changes:
    /// <list type="bullet">
    ///   <item><description>All plot models are rebuilt with appropriate colors</description></item>
    ///   <item><description>Composition chart is repainted with current values</description></item>
    ///   <item><description>Gauge charts are repainted with last known results</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="ShellViewModel"/> should call <see cref="SetTheme"/> when the
    /// application theme changes to keep all visualizations synchronized.
    /// </para>
    /// </remarks>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (!Set(ref _isDarkTheme, value))
                return;

            RebuildPlotsForTheme();

            // Repaint based on current values
            UpdateCompositionPlot();
            UpdateGaugeModels(_lastHasResult, _lastGraphScore01, _lastHbMin, _lastHbMax);
        }
    }

    /// <summary>
    /// Sets the chart theme for OxyPlot visualizations.
    /// </summary>
    /// <param name="isDark">
    /// <c>true</c> to enable dark theme; <c>false</c> for light theme.
    /// </param>
    /// <remarks>
    /// This method should be called by the <see cref="ShellViewModel"/> when the
    /// application theme changes to ensure consistent theming across the UI.
    /// </remarks>
    public void SetTheme(bool isDark) => IsDarkTheme = isDark;

    // -----------------------------
    // Inputs: Composition (wt%)
    // -----------------------------

    private double _carbon;
    /// <summary>
    /// Gets or sets the carbon content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The carbon percentage. Valid range: 0.0 - 5.0 wt%.
    /// </value>
    /// <remarks>
    /// Carbon is the primary graphite-forming element. Typical range for gray iron is 2.5-4.0 wt%.
    /// When this value changes, the composition chart is automatically updated.
    /// </remarks>
    public double Carbon
    {
        get => _carbon;
        set
        {
            if (Set(ref _carbon, value))
                UpdateCompositionPlot();
        }
    }

    private double _silicon;
    /// <summary>
    /// Gets or sets the silicon content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The silicon percentage. Valid range: 0.0 - 5.0 wt%.
    /// </value>
    /// <remarks>
    /// Silicon is a strong graphite promoter. Typical range for gray iron is 1.0-3.0 wt%.
    /// When this value changes, the composition chart is automatically updated.
    /// </remarks>
    public double Silicon
    {
        get => _silicon;
        set
        {
            if (Set(ref _silicon, value))
                UpdateCompositionPlot();
        }
    }

    private double _manganese;
    /// <summary>
    /// Gets or sets the manganese content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The manganese percentage. Valid range: 0.0 - 3.0 wt%.
    /// </value>
    /// <remarks>
    /// Manganese is a pearlite stabilizer. Typical range for gray iron is 0.4-1.2 wt%.
    /// When this value changes, the composition chart is automatically updated.
    /// </remarks>
    public double Manganese
    {
        get => _manganese;
        set
        {
            if (Set(ref _manganese, value))
                UpdateCompositionPlot();
        }
    }

    private double _phosphorus;
    /// <summary>
    /// Gets or sets the phosphorus content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The phosphorus percentage. Valid range: 0.0 - 1.0 wt%.
    /// </value>
    /// <remarks>
    /// Phosphorus contributes to carbon equivalent and improves fluidity. Typical range for gray iron is 0.02-0.15 wt%.
    /// When this value changes, the composition chart is automatically updated.
    /// </remarks>
    public double Phosphorus
    {
        get => _phosphorus;
        set
        {
            if (Set(ref _phosphorus, value))
                UpdateCompositionPlot();
        }
    }

    private double _sulfur;
    /// <summary>
    /// Gets or sets the sulfur content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The sulfur percentage. Valid range: 0.0 - 1.0 wt%.
    /// </value>
    /// <remarks>
    /// Sulfur is a carbide stabilizer. Typical range for gray iron is 0.05-0.15 wt%.
    /// When this value changes, the composition chart is automatically updated.
    /// </remarks>
    public double Sulfur
    {
        get => _sulfur;
        set
        {
            if (Set(ref _sulfur, value))
                UpdateCompositionPlot();
        }
    }

    // -----------------------------
    // Inputs: Section
    // -----------------------------

    private double _thicknessMm;
    /// <summary>
    /// Gets or sets the section thickness in millimeters.
    /// </summary>
    /// <value>
    /// The section thickness. Must be greater than zero.
    /// </value>
    /// <remarks>
    /// Section thickness affects cooling rate and microstructure development.
    /// Thinner sections cool faster and are more prone to chill formation.
    /// </remarks>
    public double ThicknessMm
    {
        get => _thicknessMm;
        set => Set(ref _thicknessMm, value);
    }

    private double _coolingRateCPerSec;
    /// <summary>
    /// Gets or sets the cooling rate in degrees Celsius per second.
    /// </summary>
    /// <value>
    /// The cooling rate in °C/s. Must be greater than zero.
    /// Typical range: 0.05-20 °C/s for most casting operations.
    /// </value>
    /// <remarks>
    /// Cooling rate significantly affects graphitization and final microstructure.
    /// Faster cooling increases chill risk and hardness.
    /// </remarks>
    public double CoolingRateCPerSec
    {
        get => _coolingRateCPerSec;
        set => Set(ref _coolingRateCPerSec, value);
    }

    // -----------------------------
    // Outputs
    // -----------------------------

    private CastIronEstimate? _result;
    /// <summary>
    /// Gets the current estimation result, or null if no calculation has been performed.
    /// </summary>
    /// <value>
    /// The <see cref="CastIronEstimate"/> containing all calculated properties and risk flags,
    /// or null if no result is available.
    /// </value>
    public CastIronEstimate? Result
    {
        get => _result;
        private set
        {
            _result = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResult));
            OnPropertyChanged(nameof(CarbonEquivalentText));
            OnPropertyChanged(nameof(GraphitizationScoreText));
            OnPropertyChanged(nameof(HardnessText));
            OnPropertyChanged(nameof(CoolingFactorText));
            OnPropertyChanged(nameof(ThicknessFactorText));
            OnPropertyChanged(nameof(Flags));
        }
    }

    /// <summary>
    /// Gets a value indicating whether a calculation result is available.
    /// </summary>
    /// <value>
    /// <c>true</c> if a result is available; otherwise, <c>false</c>.
    /// </value>
    public bool HasResult => Result is not null;

    /// <summary>
    /// Gets the formatted carbon equivalent text for display.
    /// </summary>
    /// <value>
    /// The carbon equivalent value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string CarbonEquivalentText => Result is null
        ? "—"
        : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted graphitization score text for display.
    /// </summary>
    /// <value>
    /// The graphitization score (0-1 scale) formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string GraphitizationScoreText => Result is null
        ? "—"
        : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted hardness range text for display.
    /// </summary>
    /// <value>
    /// The hardness range in Brinell (e.g., "205-235 HB"), or "—" if no result is available.
    /// </value>
    public string HardnessText => Result is null
        ? "—"
        : Result.EstimatedHardness.ToString();

    /// <summary>
    /// Gets the collection of risk flags identified during estimation.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="RiskFlag"/> instances, or an empty list if no result is available.
    /// </value>
    public IReadOnlyList<RiskFlag> Flags => Result?.Flags ?? Array.Empty<RiskFlag>();

    // -----------------------------
    // Commands
    // -----------------------------

    /// <summary>
    /// Gets the command to execute the cast iron property estimation.
    /// </summary>
    /// <remarks>
    /// This command validates all inputs, executes the estimation calculation,
    /// updates visualizations, and displays appropriate status messages.
    /// </remarks>
    public ICommand CalculateCommand { get; }

    /// <summary>
    /// Gets the command to clear the current estimation result.
    /// </summary>
    /// <remarks>
    /// This command clears the result, resets gauge visualizations, and
    /// returns the status to "Ready" state.
    /// </remarks>
    public ICommand ClearCommand { get; }

    /// <summary>
    /// Executes the cast iron property estimation calculation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    ///   <item><description>Validates all input parameters</description></item>
    ///   <item><description>If validation fails, clears result and displays warning</description></item>
    ///   <item><description>Builds domain input models from UI inputs</description></item>
    ///   <item><description>Calls the estimation service</description></item>
    ///   <item><description>Updates all visualizations with results</description></item>
    ///   <item><description>Displays success status or error message</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Status messages reflect the severity of any risk flags identified.
    /// </para>
    /// </remarks>
    private void Calculate()
    {
        var issues = ValidateInputs();
        if (issues.Count > 0)
        {
            Result = null;
            UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
            _status.Set(AppStatusLevel.Warning, "Check inputs", issues[0]);
            return;
        }

        try
        {
            var inputs = new CastIronInputs(
                Composition: BuildComposition(),
                Section: BuildSection());

            var estimate = _estimator.Estimate(inputs);
            Result = estimate;

            UpdateGaugeModels(
                hasResult: true,
                graphScore01: estimate.GraphitizationScore,
                hbMin: estimate.EstimatedHardness.MinHB,
                hbMax: estimate.EstimatedHardness.MaxHB);

            var right = estimate.Flags.Count == 0 ? "No risks" : $"{estimate.Flags.Count} risk(s)";
            var level = AnyHighSeverity(estimate.Flags) ? AppStatusLevel.Warning : AppStatusLevel.Ok;

            _status.Set(level, "Calculated", right);
        }
        catch (Exception ex)
        {
            Result = null;
            UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
            _status.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
        }
    }

    /// <summary>
    /// Clears the current estimation result and resets the UI to ready state.
    /// </summary>
    private void Clear()
    {
        Result = null;
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // -----------------------------
    // Domain Builders
    // -----------------------------

    /// <summary>
    /// Builds a <see cref="CastIronComposition"/> from the current input values.
    /// </summary>
    /// <returns>A new <see cref="CastIronComposition"/> instance.</returns>
    private CastIronComposition BuildComposition()
        => new(
            Carbon: Carbon,
            Silicon: Silicon,
            Manganese: Manganese,
            Phosphorus: Phosphorus,
            Sulfur: Sulfur);

    /// <summary>
    /// Builds a <see cref="SectionProfile"/> from the current input values.
    /// </summary>
    /// <returns>A new <see cref="SectionProfile"/> instance.</returns>
    private SectionProfile BuildSection()
        => new(
            ThicknessMm: ThicknessMm,
            CoolingRateCPerSec: CoolingRateCPerSec);

    /// <summary>
    /// Determines whether any risk flags have high severity.
    /// </summary>
    /// <param name="flags">The collection of risk flags to check.</param>
    /// <returns>
    /// <c>true</c> if any flag has <see cref="RiskSeverity.High"/>; otherwise, <c>false</c>.
    /// </returns>
    private static bool AnyHighSeverity(IReadOnlyList<RiskFlag> flags)
    {
        foreach (var f in flags)
            if (f.Severity == RiskSeverity.High)
                return true;

        return false;
    }

    // -----------------------------
    // Validation
    // -----------------------------

    /// <summary>
    /// Validates all input parameters and returns a list of validation issues.
    /// </summary>
    /// <returns>
    /// A list of validation error messages. Empty list indicates all inputs are valid.
    /// </returns>
    private List<string> ValidateInputs()
    {
        var issues = new List<string>();

        if (Carbon is < CarbonMin or > CarbonMax)
            issues.Add($"Carbon must be between {CarbonMin:0.##} and {CarbonMax:0.##}.");

        if (Silicon is < SiliconMin or > SiliconMax)
            issues.Add($"Silicon must be between {SiliconMin:0.##} and {SiliconMax:0.##}.");

        if (Manganese is < ManganeseMin or > ManganeseMax)
            issues.Add($"Manganese must be between {ManganeseMin:0.##} and {ManganeseMax:0.##}.");

        if (Phosphorus is < PhosphorusMin or > PhosphorusMax)
            issues.Add($"Phosphorus must be between {PhosphorusMin:0.##} and {PhosphorusMax:0.##}.");

        if (Sulfur is < SulfurMin or > SulfurMax)
            issues.Add($"Sulfur must be between {SulfurMin:0.##} and {SulfurMax:0.##}.");

        if (ThicknessMm <= 0)
            issues.Add("Thickness must be greater than 0 mm.");

        if (CoolingRateCPerSec <= 0)
            issues.Add("Cooling rate must be greater than 0 (°C/s).");

        return issues;
    }

    // ============================================================
    // OxyPlot Models (theme-aware)
    // ============================================================

    private PlotModel? _compositionPlotModel;
    /// <summary>
    /// Gets the plot model for the composition bar chart.
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying chemical composition as a bar chart.
    /// </value>
    public PlotModel CompositionPlotModel
    {
        get => _compositionPlotModel!;
        private set
        {
            _compositionPlotModel = value;
            OnPropertyChanged();
        }
    }

    private PlotModel? _graphGaugeModel;
    /// <summary>
    /// Gets the plot model for the graphitization gauge (donut chart).
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying graphitization score as a gauge.
    /// </value>
    public PlotModel GraphGaugeModel
    {
        get => _graphGaugeModel!;
        private set
        {
            _graphGaugeModel = value;
            OnPropertyChanged();
        }
    }

    private PlotModel? _hardnessGaugeModel;
    /// <summary>
    /// Gets the plot model for the hardness gauge (donut chart).
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying hardness as a gauge.
    /// </value>
    public PlotModel HardnessGaugeModel
    {
        get => _hardnessGaugeModel!;
        private set
        {
            _hardnessGaugeModel = value;
            OnPropertyChanged();
        }
    }

    // Series references that belong to the *current* PlotModels
    private BarSeries? _compositionSeries;
    private PieSeries? _graphGaugeSeries;
    private PieSeries? _hardnessGaugeSeries;

    // Remember the last gauge values so we can repaint when theme changes
    private bool _lastHasResult;
    private double _lastGraphScore01;
    private int _lastHbMin;
    private int _lastHbMax;

    /// <summary>
    /// Rebuilds all plot models and series for the current theme.
    /// </summary>
    /// <remarks>
    /// This method is called when the theme changes to ensure all visualizations
    /// use appropriate colors and styling for dark or light mode.
    /// </remarks>
    private void RebuildPlotsForTheme()
    {
        CompositionPlotModel = BuildCompositionModel(IsDarkTheme, out _compositionSeries);
        GraphGaugeModel = BuildGaugeModel(IsDarkTheme, out _graphGaugeSeries);
        HardnessGaugeModel = BuildGaugeModel(IsDarkTheme, out _hardnessGaugeSeries);
    }

    /// <summary>
    /// Builds a composition bar chart plot model with appropriate theming.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme colors.</param>
    /// <param name="compositionSeries">Outputs the bar series for composition data.</param>
    /// <returns>A configured <see cref="PlotModel"/> for the composition chart.</returns>
    private PlotModel BuildCompositionModel(bool isDark, out BarSeries compositionSeries)
    {
        var model = NewThemedModel(isDark);

        var text = isDark ? OxyColor.FromRgb(230, 230, 230) : OxyColor.FromRgb(20, 20, 20);
        var axisLine = isDark ? OxyColor.FromArgb(120, 255, 255, 255) : OxyColor.FromArgb(200, 0, 0, 0);
        var grid = isDark ? OxyColor.FromArgb(30, 255, 255, 255) : OxyColor.FromArgb(70, 0, 0, 0);

        var categoryAxis = new CategoryAxis
        {
            Key = "y1",
            Position = AxisPosition.Bottom,
            GapWidth = 0.35,
            ItemsSource = new[] { "C", "Si", "Mn", "P", "S" },
            TextColor = text,
            TitleColor = text,
            AxislineColor = axisLine,
            TicklineColor = axisLine,
            FontSize = isDark ? 11 : 12,
            MajorGridlineStyle = LineStyle.None,
            MinorGridlineStyle = LineStyle.None
        };

        var valueAxis = new LinearAxis
        {
            Key = "x1",
            Position = AxisPosition.Left,
            Minimum = 0,
            Title = "wt%",
            TextColor = text,
            TitleColor = text,
            AxislineColor = axisLine,
            TicklineColor = axisLine,
            FontSize = isDark ? 11 : 12,
            TitleFontSize = isDark ? 12 : 13,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = grid,
            MinorGridlineStyle = LineStyle.None
        };

        compositionSeries = new BarSeries
        {
            XAxisKey = "x1",
            YAxisKey = "y1",
            FillColor = isDark
                ? OxyColor.FromArgb(220, 90, 150, 180)
                : OxyColor.FromArgb(255, 35, 110, 170),
            StrokeColor = isDark
                ? OxyColor.FromArgb(90, 255, 255, 255)
                : OxyColor.FromArgb(120, 0, 0, 0),
            StrokeThickness = 1
        };

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        model.Series.Add(compositionSeries);

        return model;
    }

    /// <summary>
    /// Builds a gauge (donut chart) plot model with appropriate theming.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme colors.</param>
    /// <param name="gaugeSeries">Outputs the pie series for gauge data.</param>
    /// <returns>A configured <see cref="PlotModel"/> for a gauge visualization.</returns>
    private PlotModel BuildGaugeModel(bool isDark, out PieSeries gaugeSeries)
    {
        var model = NewThemedModel(isDark);

        model.Title = string.Empty;
        model.PlotMargins = new OxyThickness(0);
        model.Padding = new OxyThickness(0);
        model.IsLegendVisible = false;

        gaugeSeries = new PieSeries
        {
            InnerDiameter = 0.65,
            Stroke = OxyColors.Transparent,
            AngleSpan = 360,
            StartAngle = -90,
            InsideLabelFormat = string.Empty,
            OutsideLabelFormat = string.Empty,
            InsideLabelColor = OxyColors.Transparent,
            TickDistance = 0,
            TickHorizontalLength = 0,
            TickRadialLength = 0
        };

        model.Series.Add(gaugeSeries);
        return model;
    }

    /// <summary>
    /// Creates a new themed plot model with appropriate background and text colors.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme colors.</param>
    /// <returns>A configured <see cref="PlotModel"/> with theme-appropriate styling.</returns>
    private static PlotModel NewThemedModel(bool isDark)
    {
        if (isDark)
        {
            return new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBackground = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.Transparent,
                TextColor = OxyColor.FromRgb(230, 230, 230),
                DefaultFontSize = 12
            };
        }

        return new PlotModel
        {
            Background = OxyColor.FromRgb(255, 255, 255),
            PlotAreaBackground = OxyColor.FromRgb(255, 255, 255),
            PlotAreaBorderColor = OxyColor.FromArgb(140, 0, 0, 0),
            TextColor = OxyColor.FromRgb(20, 20, 20),
            DefaultFontSize = 13
        };
    }

    /// <summary>
    /// Updates the composition bar chart with current input values.
    /// </summary>
    /// <remarks>
    /// This method is called automatically when any composition property changes.
    /// Values are clamped to valid ranges before display.
    /// </remarks>
    private void UpdateCompositionPlot()
    {
        if (_compositionPlotModel is null || _compositionSeries is null)
            return;

        _compositionSeries.Items.Clear();

        _compositionSeries.Items.Add(new BarItem { Value = ClampTo(Carbon, 0, 5) });
        _compositionSeries.Items.Add(new BarItem { Value = ClampTo(Silicon, 0, 5) });
        _compositionSeries.Items.Add(new BarItem { Value = ClampTo(Manganese, 0, 3) });
        _compositionSeries.Items.Add(new BarItem { Value = ClampTo(Phosphorus, 0, 1) });
        _compositionSeries.Items.Add(new BarItem { Value = ClampTo(Sulfur, 0, 1) });

        _compositionPlotModel.InvalidatePlot(true);
    }

    /// <summary>
    /// Updates both gauge visualizations (graphitization and hardness) with result values.
    /// </summary>
    /// <param name="hasResult">Whether a valid result is available.</param>
    /// <param name="graphScore01">The graphitization score (0-1 scale).</param>
    /// <param name="hbMin">The minimum hardness value in Brinell.</param>
    /// <param name="hbMax">The maximum hardness value in Brinell.</param>
    /// <remarks>
    /// This method caches the last values to support theme changes without recalculation.
    /// Hardness values are normalized to a 0-1 scale for gauge display.
    /// </remarks>
    private void UpdateGaugeModels(bool hasResult, double graphScore01, int hbMin, int hbMax)
    {
        // Remember for theme re-application
        _lastHasResult = hasResult;
        _lastGraphScore01 = graphScore01;
        _lastHbMin = hbMin;
        _lastHbMax = hbMax;

        if (_graphGaugeModel is null || _graphGaugeSeries is null ||
            _hardnessGaugeModel is null || _hardnessGaugeSeries is null)
        {
            return;
        }

        UpdateDonut(
            model: _graphGaugeModel,
            series: _graphGaugeSeries,
            value01: hasResult ? Clamp01(graphScore01) : 0.0,
            fill: IsDarkTheme
                ? OxyColor.FromArgb(230, 80, 200, 120)
                : OxyColor.FromArgb(255, 30, 150, 80),
            isDarkTheme: IsDarkTheme);

        // Normalize HB midpoint into [0..1] using a stable window
        const double hbMinWindow = 140.0;
        const double hbMaxWindow = 320.0;

        double hbMid = 0.0;
        if (hasResult && hbMax >= hbMin && hbMin > 0)
            hbMid = (hbMin + hbMax) / 2.0;

        var hbNorm = hasResult
            ? Clamp01((hbMid - hbMinWindow) / (hbMaxWindow - hbMinWindow))
            : 0.0;

        UpdateDonut(
            model: _hardnessGaugeModel,
            series: _hardnessGaugeSeries,
            value01: hbNorm,
            fill: IsDarkTheme
                ? OxyColor.FromArgb(230, 120, 170, 220)
                : OxyColor.FromArgb(255, 45, 105, 200),
            isDarkTheme: IsDarkTheme);
    }

    /// <summary>
    /// Updates a donut (gauge) chart with a normalized value.
    /// </summary>
    /// <param name="model">The plot model containing the gauge.</param>
    /// <param name="series">The pie series representing the gauge.</param>
    /// <param name="value01">The normalized value (0-1 scale) to display.</param>
    /// <param name="fill">The color for the filled portion of the gauge.</param>
    /// <param name="isDarkTheme">Whether dark theme is active (affects remaining portion color).</param>
    private static void UpdateDonut(PlotModel model, PieSeries series, double value01, OxyColor fill, bool isDarkTheme)
    {
        series.Slices.Clear();

        series.Slices.Add(new PieSlice(string.Empty, value01) { Fill = fill });

        var remainingFill = isDarkTheme
            ? OxyColor.FromArgb(28, 255, 255, 255)
            : OxyColor.FromArgb(90, 0, 0, 0);

        series.Slices.Add(new PieSlice(string.Empty, Math.Max(0, 1.0 - value01))
        {
            Fill = remainingFill
        });

        model.InvalidatePlot(true);
    }

    /// <summary>
    /// Clamps a value to the range [0, 1].
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    /// <summary>
    /// Clamps a value to a specified range, treating NaN and Infinity as the minimum.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <param name="min">The minimum allowable value.</param>
    /// <param name="max">The maximum allowable value.</param>
    /// <returns>The clamped value.</returns>
    private static double ClampTo(double v, double min, double max)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return min;
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    // -----------------------------
    // INotifyPropertyChanged helpers
    // -----------------------------

    /// <summary>
    /// Sets a field value and raises property changed notification if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="name">
    /// The name of the property (automatically provided by the compiler via CallerMemberName).
    /// </param>
    /// <returns>
    /// <c>true</c> if the value changed and notification was raised; otherwise, <c>false</c>.
    /// </returns>
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="name">
    /// The name of the property that changed (automatically provided by the compiler via CallerMemberName).
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}