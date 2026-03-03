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
using Castara.Domain.Estimation.Validation;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Infrastructure.Components;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The view model for the calculations view, managing cast iron composition inputs,
/// estimation calculations, result visualizations, and real-time validation with unit system support.
/// </summary>
/// <remarks>
/// <para>
/// This view model orchestrates the entire cast iron analysis workflow with advanced features:
/// <list type="bullet">
///   <item><description>Real-time validation using <see cref="IDataErrorInfo"/> with field-level error messages</description></item>
///   <item><description>Text-based input with parsing and numeric storage separation</description></item>
///   <item><description>Bidirectional unit conversion between Standard (SI) and American Standard units</description></item>
///   <item><description>Calculation execution with comprehensive error handling and logging</description></item>
///   <item><description>Result display with formatted text properties for UI binding</description></item>
///   <item><description>Chart visualization (composition bars, graphitization/hardness gauges)</description></item>
///   <item><description>Theme switching via <see cref="IThemeAware"/> for consistent dark/light mode</description></item>
///   <item><description>Diagnostic logging at multiple levels (Trace, Debug, Info, Warning, Error)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Unit System Architecture:</strong> The view model maintains a strict separation:
/// <list type="bullet">
///   <item><description>Canonical values are always stored in SI units (mm, °C/s)</description></item>
///   <item><description>Display text reflects the current <see cref="UnitSystem"/> (mm/in, °C/s/°F/s)</description></item>
///   <item><description>User input is validated in display units, then converted to SI for storage</description></item>
///   <item><description>Calculations always use canonical SI values regardless of display units</description></item>
///   <item><description>Changing unit systems rebuilds validators and re-seeds display text from canonical values</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Validation Architecture:</strong> Three-layer validation approach:
/// <list type="number">
///   <item><description>Text properties (e.g., <see cref="CarbonText"/>) store raw user input</description></item>
///   <item><description>Numeric properties (e.g., <see cref="Carbon"/>) store validated SI values</description></item>
///   <item><description><see cref="NumericTextField"/> helper performs parsing, range validation in display units</description></item>
///   <item><description><see cref="IDataErrorInfo"/> exposes errors to WPF validation system</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Chart Management:</strong> OxyPlot charts are rebuilt on theme changes and updated
/// reactively as inputs change. Gauge values are cached to survive theme switches without recalculation.
/// </para>
/// </remarks>
public sealed class CalculationsViewModel : INotifyPropertyChanged, IThemeAware, IUnitAware, IDataErrorInfo
{
    // ============================================================
    // Constants - Green Sand Gray Iron Defaults
    // ============================================================

    /// <summary>Minimum section thickness in millimeters for green sand gray iron castings.</summary>
    private const double ThicknessMinMm = 3.0;

    /// <summary>Maximum section thickness in millimeters for green sand gray iron castings.</summary>
    private const double ThicknessMaxMm = 150.0;

    /// <summary>Minimum cooling rate in °C/s for green sand gray iron castings.</summary>
    private const double CoolingMinCPerSec = 0.01;

    /// <summary>Maximum cooling rate in °C/s for green sand gray iron castings.</summary>
    private const double CoolingMaxCPerSec = 2.0;

    /// <summary>Conversion factor: millimeters per inch.</summary>
    private const double MmPerIn = 25.4;

    /// <summary>Conversion factor: °F/s per °C/s (9/5).</summary>
    private const double FPerC = 9.0 / 5.0;

    /// <summary>Minimum hardness value for gauge display normalization (HB).</summary>
    private const double HbMinWindow = 140.0;

    /// <summary>Maximum hardness value for gauge display normalization (HB).</summary>
    private const double HbMaxWindow = 320.0;

    /// <summary>Display format for thickness in millimeters.</summary>
    private const string ThicknessFormat_Mm = "0.#";

    /// <summary>Display format for thickness in inches.</summary>
    private const string ThicknessFormat_In = "0.###";

    /// <summary>Display format for cooling rate in °C/s.</summary>
    private const string CoolingFormat_CPerSec = "0.####";

    /// <summary>Display format for cooling rate in °F/s.</summary>
    private const string CoolingFormat_FPerSec = "0.####";

    // ============================================================
    // Fields - Services
    // ============================================================

    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;
    private readonly ILogger<CalculationsViewModel> _log;

    // ============================================================
    // Fields - Validation
    // ============================================================

    private readonly NumericTextField _carbonField;
    private readonly NumericTextField _siliconField;
    private readonly NumericTextField _manganeseField;
    private readonly NumericTextField _phosphorusField;
    private readonly NumericTextField _sulfurField;

    /// <summary>
    /// Thickness field - rebuilt when unit system changes to validate in display units.
    /// </summary>
    private NumericTextField _thicknessField;

    /// <summary>
    /// Cooling rate field - rebuilt when unit system changes to validate in display units.
    /// </summary>
    private NumericTextField _coolingField;

    /// <summary>
    /// Maps WPF property names to field accessors for <see cref="IDataErrorInfo"/> support.
    /// Uses functions to allow dynamic field replacement when unit system changes.
    /// </summary>
    private readonly Dictionary<string, Func<NumericTextField>> _fieldAccessorByProperty;

    // ============================================================
    // Fields - State
    // ============================================================

    private bool _isDarkTheme = true;
    private UnitSystem _unitSystem = UnitSystem.Standard;
    private CastIronEstimate? _result;

    private PlotModel? _compositionPlotModel;
    private PlotModel? _graphGaugeModel;
    private PlotModel? _hardnessGaugeModel;

    private BarSeries? _compositionSeries;
    private PieSeries? _graphGaugeSeries;
    private PieSeries? _hardnessGaugeSeries;

    /// <summary>Cached flag: whether last gauge update had a valid result.</summary>
    private bool _lastHasResult;

    /// <summary>Cached graphitization score for theme-change repainting.</summary>
    private double _lastGraphScore01;

    /// <summary>Cached minimum hardness value for theme-change repainting.</summary>
    private int _lastHbMin;

    /// <summary>Cached maximum hardness value for theme-change repainting.</summary>
    private int _lastHbMax;

    // ============================================================
    // Events
    // ============================================================

    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ============================================================
    // Constructor
    // ============================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsViewModel"/> class.
    /// </summary>
    /// <param name="status">The status service for displaying operation results.</param>
    /// <param name="estimator">The cast iron estimator service for performing calculations.</param>
    /// <param name="log">The logger for diagnostic telemetry (optional, defaults to null logger).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="status"/> or <paramref name="estimator"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor performs comprehensive initialization:
    /// <list type="number">
    ///   <item><description>Creates validation fields for all inputs with range constraints</description></item>
    ///   <item><description>Builds unit-sensitive validators for thickness and cooling rate</description></item>
    ///   <item><description>Sets default values (typical Class 30 gray iron composition)</description></item>
    ///   <item><description>Seeds text fields from numeric defaults in current unit system</description></item>
    ///   <item><description>Builds OxyPlot chart models for current theme</description></item>
    ///   <item><description>Updates composition chart with default values</description></item>
    ///   <item><description>Initializes gauge charts to zero state</description></item>
    ///   <item><description>Sets initial status to "Ready"</description></item>
    ///   <item><description>Logs initialization at Information level</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Default Composition:</strong> Typical Class 30 gray iron with C: 3.40%, Si: 2.10%,
    /// Mn: 0.55%, P: 0.05%, S: 0.02%, Section: 12mm thickness, 1°C/s cooling rate.
    /// </para>
    /// </remarks>
    public CalculationsViewModel(
        IStatusService status,
        ICastIronEstimator estimator,
        ILogger<CalculationsViewModel>? log = null)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _log = log ?? NullLogger<CalculationsViewModel>.Instance;

        CalculateCommand = new RelayCommand(Calculate, CanCalculate);
        ClearCommand = new RelayCommand(Clear);

        // Composition fields: use domain constraints (wt% always same units)
        _carbonField = NumericTextField.Range("Carbon", CastIronInputConstraints.CarbonMin, CastIronInputConstraints.CarbonMax);
        _siliconField = NumericTextField.Range("Silicon", CastIronInputConstraints.SiliconMin, CastIronInputConstraints.SiliconMax);
        _manganeseField = NumericTextField.Range("Manganese", CastIronInputConstraints.ManganeseMin, CastIronInputConstraints.ManganeseMax);
        _phosphorusField = NumericTextField.Range("Phosphorus", CastIronInputConstraints.PhosphorusMin, CastIronInputConstraints.PhosphorusMax);
        _sulfurField = NumericTextField.Range("Sulfur", CastIronInputConstraints.SulfurMin, CastIronInputConstraints.SulfurMax);

        // Unit-sensitive: build for initial UnitSystem
        _thicknessField = BuildThicknessField(_unitSystem);
        _coolingField = BuildCoolingField(_unitSystem);

        // Field mapping for IDataErrorInfo
        _fieldAccessorByProperty = new()
        {
            { nameof(CarbonText), () => _carbonField },
            { nameof(SiliconText), () => _siliconField },
            { nameof(ManganeseText), () => _manganeseField },
            { nameof(PhosphorusText), () => _phosphorusField },
            { nameof(SulfurText), () => _sulfurField },
            { nameof(ThicknessText), () => _thicknessField },
            { nameof(CoolingRateText), () => _coolingField },
        };

        ApplyDefaultNumerics();
        SeedAllTextFromNumerics(); // Seeds DISPLAY values from canonical SI

        RebuildPlotsForTheme();
        UpdateCompositionPlot();
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
        _log.LogInformation("CalculationsViewModel initialized");
    }

    // ============================================================
    // Properties - Canonical Numeric Values (Always SI Units)
    // ============================================================

    /// <summary>
    /// Gets the validated carbon content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The carbon percentage. This value is only updated when <see cref="CarbonText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double Carbon { get; private set; }

    /// <summary>
    /// Gets the validated silicon content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The silicon percentage. This value is only updated when <see cref="SiliconText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double Silicon { get; private set; }

    /// <summary>
    /// Gets the validated manganese content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The manganese percentage. This value is only updated when <see cref="ManganeseText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double Manganese { get; private set; }

    /// <summary>
    /// Gets the validated phosphorus content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The phosphorus percentage. This value is only updated when <see cref="PhosphorusText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double Phosphorus { get; private set; }

    /// <summary>
    /// Gets the validated sulfur content in weight percent (wt%).
    /// </summary>
    /// <value>
    /// The sulfur percentage. This value is only updated when <see cref="SulfurText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double Sulfur { get; private set; }

    /// <summary>
    /// Gets the validated section thickness in millimeters (canonical SI value).
    /// </summary>
    /// <value>
    /// The thickness in mm. This value is only updated when <see cref="ThicknessText"/> parses
    /// successfully and passes validation in display units, then converts to mm.
    /// </value>
    public double ThicknessValue { get; private set; }

    /// <summary>
    /// Gets the validated cooling rate in degrees Celsius per second (canonical SI value).
    /// </summary>
    /// <value>
    /// The cooling rate in °C/s. This value is only updated when <see cref="CoolingRateText"/> parses
    /// successfully and passes validation in display units, then converts to °C/s.
    /// </value>
    public double CoolingRateValue { get; private set; }

    // ============================================================
    // Properties - Tooltips and Labels (Unit-Aware)
    // ============================================================

    /// <summary>
    /// Gets the tooltip text for the carbon input field with validation range.
    /// </summary>
    public string CarbonTooltip =>
        $"Carbon (C), wt%.\nValid range: {CastIronInputConstraints.CarbonMin:0.##} – {CastIronInputConstraints.CarbonMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the silicon input field with validation range.
    /// </summary>
    public string SiliconTooltip =>
        $"Silicon (Si), wt%.\nValid range: {CastIronInputConstraints.SiliconMin:0.##} – {CastIronInputConstraints.SiliconMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the manganese input field with validation range.
    /// </summary>
    public string ManganeseTooltip =>
        $"Manganese (Mn), wt%.\nValid range: {CastIronInputConstraints.ManganeseMin:0.##} – {CastIronInputConstraints.ManganeseMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the phosphorus input field with validation range.
    /// </summary>
    public string PhosphorusTooltip =>
        $"Phosphorus (P), wt%.\nValid range: {CastIronInputConstraints.PhosphorusMin:0.##} – {CastIronInputConstraints.PhosphorusMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the sulfur input field with validation range.
    /// </summary>
    public string SulfurTooltip =>
        $"Sulfur (S), wt%.\nValid range: {CastIronInputConstraints.SulfurMin:0.##} – {CastIronInputConstraints.SulfurMax:0.##}.";

    /// <summary>
    /// Gets the tooltip text for the thickness input field with unit-appropriate validation range.
    /// </summary>
    /// <value>
    /// Displays range in inches when American Standard units are active, otherwise in millimeters.
    /// Includes note about green sand gray iron defaults.
    /// </value>
    public string ThicknessTooltip
        => UnitSystem == UnitSystem.AmericanStandard
            ? $"Section thickness in inches (in).\nValid range: {ThicknessMinMm / MmPerIn:0.###} – {ThicknessMaxMm / MmPerIn:0.###} in.\n(Green sand gray iron defaults.)"
            : $"Section thickness in millimeters (mm).\nValid range: {ThicknessMinMm:0.##} – {ThicknessMaxMm:0.##} mm.\n(Green sand gray iron defaults.)";

    /// <summary>
    /// Gets the tooltip text for the cooling rate input field with unit-appropriate validation range.
    /// </summary>
    /// <value>
    /// Displays range in °F/s when American Standard units are active, otherwise in °C/s.
    /// Includes note about green sand gray iron defaults.
    /// </value>
    public string CoolingRateTooltip
        => UnitSystem == UnitSystem.AmericanStandard
            ? $"Cooling rate in °F/s.\nValid range: {CoolingMinCPerSec * FPerC:0.###} – {CoolingMaxCPerSec * FPerC:0.###} °F/s.\n(Green sand gray iron defaults.)"
            : $"Cooling rate in °C/s.\nValid range: {CoolingMinCPerSec:0.###} – {CoolingMaxCPerSec:0.###} °C/s.\n(Green sand gray iron defaults.)";

    /// <summary>
    /// Gets the label text for the thickness input field with appropriate units.
    /// </summary>
    /// <value>
    /// Returns "Thickness (in)" for American Standard, "Thickness (mm)" for Standard units.
    /// </value>
    public string ThicknessLabel => UnitSystem == UnitSystem.AmericanStandard ? "Thickness (in)" : "Thickness (mm)";

    /// <summary>
    /// Gets the label text for the cooling rate input field with appropriate units.
    /// </summary>
    /// <value>
    /// Returns "Cooling Rate (°F/s)" for American Standard, "Cooling Rate (°C/s)" for Standard units.
    /// </value>
    public string CoolingRateLabel => UnitSystem == UnitSystem.AmericanStandard ? "Cooling Rate (°F/s)" : "Cooling Rate (°C/s)";

    /// <summary>
    /// Gets the unit suffix text for cooling rate display.
    /// </summary>
    /// <value>
    /// Returns "°F/s" for American Standard, "°C/s" for Standard units.
    /// </value>
    public string CoolingRateUnitSuffix => UnitSystem == UnitSystem.AmericanStandard ? "°F/s" : "°C/s";

    // ============================================================
    // Properties - Validation
    // ============================================================

    /// <summary>
    /// Gets a value indicating whether all input fields contain valid values.
    /// </summary>
    /// <value>
    /// <c>true</c> if all fields pass validation; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property is used to enable/disable the Calculate button. It checks
    /// all seven input fields (5 composition + 2 section parameters) for validity.
    /// </remarks>
    public bool IsValid => CanCalculate();

    /// <summary>
    /// Gets an error message indicating what is wrong with this object.
    /// </summary>
    /// <value>
    /// Always returns an empty string as validation is handled at the property level.
    /// </value>
    string IDataErrorInfo.Error => string.Empty;

    /// <summary>
    /// Gets the error message for the property with the given name.
    /// </summary>
    /// <param name="columnName">The name of the property whose error message to get.</param>
    /// <returns>
    /// The error message for the property, or an empty string if the property is valid
    /// or not recognized.
    /// </returns>
    /// <remarks>
    /// This indexer enables WPF's validation system to display field-specific error messages
    /// in tooltips, borders, or other validation UI elements. Uses dynamic field accessors
    /// to support field replacement when unit system changes.
    /// </remarks>
    string IDataErrorInfo.this[string columnName]
        => _fieldAccessorByProperty.TryGetValue(columnName, out var getter)
            ? getter().Error
            : string.Empty;

    // ============================================================
    // Properties - Text Input Wrappers (Display Units)
    // ============================================================

    /// <summary>
    /// Gets or sets the carbon input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="Carbon"/> is updated and the composition chart is refreshed.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string CarbonText
    {
        get => _carbonField.Text;
        set => SetFieldText(_carbonField, value, v => Carbon = v, nameof(CarbonText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the silicon input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="Silicon"/> is updated and the composition chart is refreshed.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string SiliconText
    {
        get => _siliconField.Text;
        set => SetFieldText(_siliconField, value, v => Silicon = v, nameof(SiliconText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the manganese input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="Manganese"/> is updated and the composition chart is refreshed.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string ManganeseText
    {
        get => _manganeseField.Text;
        set => SetFieldText(_manganeseField, value, v => Manganese = v, nameof(ManganeseText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the phosphorus input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="Phosphorus"/> is updated and the composition chart is refreshed.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string PhosphorusText
    {
        get => _phosphorusField.Text;
        set => SetFieldText(_phosphorusField, value, v => Phosphorus = v, nameof(PhosphorusText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the sulfur input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="Sulfur"/> is updated and the composition chart is refreshed.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string SulfurText
    {
        get => _sulfurField.Text;
        set => SetFieldText(_sulfurField, value, v => Sulfur = v, nameof(SulfurText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the thickness input text for UI binding in current display units.
    /// </summary>
    /// <value>
    /// The raw text entered by the user in display units (inches or millimeters). May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property demonstrates the bidirectional unit conversion pattern:
    /// <list type="bullet">
    ///   <item><description>Text is validated in display units (in or mm depending on <see cref="UnitSystem"/>)</description></item>
    ///   <item><description>Valid values are converted to canonical SI (mm) and stored in <see cref="ThicknessValue"/></description></item>
    ///   <item><description>Validation ranges adjust to display units automatically</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string ThicknessText
    {
        get => _thicknessField.Text;
        set => SetFieldText(
            _thicknessField,
            value,
            vDisplay => ThicknessValue = ToMmFromDisplay(vDisplay, UnitSystem),
            nameof(ThicknessText),
            afterValid: () => OnPropertyChanged(nameof(ThicknessValue)));
    }

    /// <summary>
    /// Gets or sets the cooling rate input text for UI binding in current display units.
    /// </summary>
    /// <value>
    /// The raw text entered by the user in display units (°F/s or °C/s). May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property demonstrates the bidirectional unit conversion pattern:
    /// <list type="bullet">
    ///   <item><description>Text is validated in display units (°F/s or °C/s depending on <see cref="UnitSystem"/>)</description></item>
    ///   <item><description>Valid values are converted to canonical SI (°C/s) and stored in <see cref="CoolingRateValue"/></description></item>
    ///   <item><description>Validation ranges adjust to display units automatically</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string CoolingRateText
    {
        get => _coolingField.Text;
        set => SetFieldText(
            _coolingField,
            value,
            vDisplay => CoolingRateValue = ToCPerSecFromDisplay(vDisplay, UnitSystem),
            nameof(CoolingRateText),
            afterValid: () => OnPropertyChanged(nameof(CoolingRateValue)));
    }

    // ============================================================
    // Properties - Results
    // ============================================================

    /// <summary>
    /// Gets the current estimation result, or null if no calculation has been performed.
    /// </summary>
    /// <value>
    /// The <see cref="CastIronEstimate"/> containing all calculated properties and risk flags,
    /// or null if no result is available.
    /// </value>
    /// <remarks>
    /// When this property changes, all derived text properties and the flags collection
    /// are automatically updated through property change notifications.
    /// </remarks>
    public CastIronEstimate? Result
    {
        get => _result;
        private set
        {
            if (ReferenceEquals(_result, value)) return;
            _result = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CarbonEquivalentText));
            OnPropertyChanged(nameof(GraphitizationScoreText));
            OnPropertyChanged(nameof(HardnessText));
            OnPropertyChanged(nameof(CoolingFactorText));
            OnPropertyChanged(nameof(ThicknessFactorText));
            OnPropertyChanged(nameof(Flags));
        }
    }

    /// <summary>
    /// Gets the formatted carbon equivalent text for display.
    /// </summary>
    /// <value>
    /// The carbon equivalent value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string CarbonEquivalentText
        => Result is null ? "—" : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted graphitization score text for display.
    /// </summary>
    /// <value>
    /// The graphitization score (0-1 scale) formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string GraphitizationScoreText
        => Result is null ? "—" : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted hardness range text for display.
    /// </summary>
    /// <value>
    /// The hardness range in Brinell (e.g., "205-235 HB"), or "—" if no result is available.
    /// </value>
    public string HardnessText
        => Result is null ? "—" : Result.EstimatedHardness.ToString();

    /// <summary>
    /// Gets the formatted cooling factor text for display.
    /// </summary>
    /// <value>
    /// The cooling factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string CoolingFactorText
        => Result is null ? "—" : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted thickness factor text for display.
    /// </summary>
    /// <value>
    /// The thickness factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string ThicknessFactorText
        => Result is null ? "—" : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the collection of risk flags identified during estimation.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="RiskFlag"/> instances, or an empty list if no result is available.
    /// </value>
    public IReadOnlyList<RiskFlag> Flags
        => Result?.Flags ?? Array.Empty<RiskFlag>();

    // ============================================================
    // Properties - Charts
    // ============================================================

    /// <summary>
    /// Gets the plot model for the composition bar chart.
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying chemical composition as a bar chart.
    /// </value>
    public PlotModel CompositionPlotModel
    {
        get => _compositionPlotModel!;
        private set { _compositionPlotModel = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets the plot model for the graphitization gauge (donut chart).
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying graphitization score as a gauge (0-1 scale).
    /// </value>
    public PlotModel GraphGaugeModel
    {
        get => _graphGaugeModel!;
        private set { _graphGaugeModel = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets the plot model for the hardness gauge (donut chart).
    /// </summary>
    /// <value>
    /// The <see cref="PlotModel"/> displaying hardness as a normalized gauge.
    /// </value>
    public PlotModel HardnessGaugeModel
    {
        get => _hardnessGaugeModel!;
        private set { _hardnessGaugeModel = value; OnPropertyChanged(); }
    }

    // ============================================================
    // Properties - Theme (IThemeAware)
    // ============================================================

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled for chart visualizations.
    /// </summary>
    /// <value>
    /// <c>true</c> if dark theme is enabled; otherwise, <c>false</c> for light theme.
    /// </value>
    /// <remarks>
    /// <para>
    /// When the theme changes:
    /// <list type="bullet">
    ///   <item><description>All plot models are rebuilt with appropriate colors</description></item>
    ///   <item><description>Composition chart is repainted with current values</description></item>
    ///   <item><description>Gauge charts are repainted with last known results</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value) return;
            _isDarkTheme = value;
            OnPropertyChanged();

            RebuildPlotsForTheme();
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
    /// This method is called by the <see cref="ShellViewModel"/> when the
    /// application theme changes to ensure consistent theming across the UI.
    /// </remarks>
    public void SetTheme(bool isDark) => IsDarkTheme = isDark;

    // ============================================================
    // Properties - Unit System (IUnitAware)
    // ============================================================

    /// <summary>
    /// Gets or sets the unit system used for displaying and interpreting section parameters.
    /// </summary>
    /// <value>
    /// The active <see cref="Models.UnitSystem"/> (Standard or American Standard).
    /// </value>
    /// <remarks>
    /// <para>
    /// When changed, this property triggers a comprehensive update:
    /// <list type="number">
    ///   <item><description>Thickness and cooling rate validators are rebuilt with display-unit ranges</description></item>
    ///   <item><description>Text fields are re-seeded from canonical SI values converted to new display units</description></item>
    ///   <item><description>All unit-dependent UI properties (tooltips, labels) are updated</description></item>
    ///   <item><description>Canonical SI values remain unchanged - only display representation changes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Critical:</strong> Calculations always use canonical SI values (<see cref="ThicknessValue"/>
    /// and <see cref="CoolingRateValue"/>) regardless of the active unit system. This property only affects
    /// validation ranges and text display.
    /// </para>
    /// </remarks>
    public UnitSystem UnitSystem
    {
        get => _unitSystem;
        set
        {
            if (_unitSystem == value) return;
            _unitSystem = value;
            OnPropertyChanged();

            // Rebuild unit-sensitive validators and reseed text from canonical SI values
            RebuildUnitSensitiveFieldsAndReseed();

            // Update unit-dependent UI properties
            OnPropertyChanged(nameof(ThicknessTooltip));
            OnPropertyChanged(nameof(CoolingRateTooltip));
            OnPropertyChanged(nameof(CoolingRateUnitSuffix));
            OnPropertyChanged(nameof(ThicknessLabel));
            OnPropertyChanged(nameof(CoolingRateLabel));
        }
    }

    // ============================================================
    // Properties - Commands
    // ============================================================

    /// <summary>
    /// Gets the command to execute the cast iron property estimation.
    /// </summary>
    /// <value>
    /// A command that validates inputs, executes calculation, updates visualizations, and displays status.
    /// </value>
    /// <remarks>
    /// The command's CanExecute updates automatically as validation state changes.
    /// Calculations always use canonical SI values regardless of display units.
    /// </remarks>
    public ICommand CalculateCommand { get; }

    /// <summary>
    /// Gets the command to clear the current estimation result and reset inputs to defaults.
    /// </summary>
    /// <value>
    /// A command that clears results, resets numeric values to defaults, re-seeds text fields, and returns to "Ready" state.
    /// </value>
    public ICommand ClearCommand { get; }

    // ============================================================
    // Private Methods - Calculation Logic
    // ============================================================

    /// <summary>
    /// Executes the cast iron property estimation calculation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    ///   <item><description>Logs calculation request at Information level</description></item>
    ///   <item><description>Validates all inputs (blocks execution if invalid)</description></item>
    ///   <item><description>Builds domain input models from canonical SI values</description></item>
    ///   <item><description>Calls the estimation service</description></item>
    ///   <item><description>Updates gauge visualizations with results</description></item>
    ///   <item><description>Displays success or warning status</description></item>
    ///   <item><description>Handles exceptions with Error logging and status display</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Calculations always use canonical SI values from <see cref="ThicknessValue"/>
    /// and <see cref="CoolingRateValue"/>, regardless of the current <see cref="UnitSystem"/>.
    /// </para>
    /// </remarks>
    private void Calculate()
    {
        _log.LogInformation("Calculation requested");

        try
        {
            if (!CanCalculate())
            {
                _status.Set(AppStatusLevel.Warning, "Check inputs", "One or more fields are invalid.");
                return;
            }

            var inputs = new CastIronInputs(
                Composition: new CastIronComposition(Carbon, Silicon, Manganese, Phosphorus, Sulfur),
                Section: new SectionProfile(ThicknessValue, CoolingRateValue));

            Result = _estimator.Estimate(inputs);

            if (Result is not null)
            {
                UpdateGaugeModels(
                    hasResult: true,
                    graphScore01: Result.GraphitizationScore,
                    hbMin: Result.EstimatedHardness.MinHB,
                    hbMax: Result.EstimatedHardness.MaxHB);

                _status.Set(AppStatusLevel.Ok, "Calculated", "OK");
            }
            else
            {
                UpdateGaugeModels(false, 0, 0, 0);
                _status.Set(AppStatusLevel.Warning, "No result", "Estimator returned no result.");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Calculation failed");
            Result = null;
            UpdateGaugeModels(false, 0, 0, 0);
            _status.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
        }
    }

    /// <summary>
    /// Clears the current estimation result and resets all inputs to default values.
    /// </summary>
    /// <remarks>
    /// This method logs the clear operation at Information level, resets all numeric
    /// properties to their default values (typical Class 30 gray iron), re-seeds the
    /// text fields in current display units, clears visualizations, and returns the status to "Ready" state.
    /// </remarks>
    private void Clear()
    {
        _log.LogInformation("Inputs cleared");

        Result = null;
        UpdateGaugeModels(false, 0, 0, 0);

        ApplyDefaultNumerics();
        SeedAllTextFromNumerics();

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    /// <summary>
    /// Applies default numeric values to all canonical properties.
    /// </summary>
    /// <remarks>
    /// Sets typical Class 30 gray iron composition: C: 3.40%, Si: 2.10%, Mn: 0.55%,
    /// P: 0.05%, S: 0.02%, with section parameters: thickness: 12mm, cooling: 1°C/s.
    /// All values are in canonical SI units.
    /// </remarks>
    private void ApplyDefaultNumerics()
    {
        // Typical Class 30 gray iron defaults
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;

        ThicknessValue = 12.0;   // mm (canonical)
        CoolingRateValue = 1.0;  // °C/s (canonical)
    }

    // ============================================================
    // Private Methods - Field Management
    // ============================================================

    /// <summary>
    /// Sets a field's text value, attempts parsing/validation, and updates the canonical numeric value if valid.
    /// </summary>
    /// <param name="field">The validation field to update.</param>
    /// <param name="value">The new text value from the UI.</param>
    /// <param name="assignIfValid">Action to update the canonical numeric property if validation succeeds.</param>
    /// <param name="propertyName">The property name for change notification.</param>
    /// <param name="refreshComposition">Whether to refresh the composition chart after a valid update.</param>
    /// <param name="afterValid">Optional action to invoke after successful validation and assignment.</param>
    /// <remarks>
    /// <para>
    /// This method coordinates the validation workflow:
    /// <list type="number">
    ///   <item><description>Updates the field's text</description></item>
    ///   <item><description>Attempts to parse and validate the text</description></item>
    ///   <item><description>If valid: updates canonical property, invokes callbacks, optionally refreshes charts</description></item>
    ///   <item><description>Raises property change notifications for text property and IsValid</description></item>
    ///   <item><description>Updates Calculate command's CanExecute state</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void SetFieldText(
        NumericTextField field,
        string? value,
        Action<double> assignIfValid,
        string propertyName,
        bool refreshComposition = false,
        Action? afterValid = null)
    {
        field.Text = value ?? string.Empty;

        if (field.TryGetValidValue(out var v))
        {
            assignIfValid(v);
            afterValid?.Invoke();

            if (refreshComposition)
                UpdateCompositionPlot();
        }

        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(IsValid));
        InvalidateCanExecute();
    }

    /// <summary>
    /// Initializes all text fields from their corresponding numeric canonical SI values,
    /// converting to current display units for thickness and cooling rate.
    /// </summary>
    /// <remarks>
    /// This method is called during construction and after clear operations to ensure
    /// the UI displays valid initial values without validation errors. Composition values
    /// are seeded directly (wt% same in all systems), while thickness and cooling rate
    /// are converted from SI to display units.
    /// </remarks>
    private void SeedAllTextFromNumerics()
    {
        _carbonField.Seed(Carbon);
        _siliconField.Seed(Silicon);
        _manganeseField.Seed(Manganese);
        _phosphorusField.Seed(Phosphorus);
        _sulfurField.Seed(Sulfur);

        // Seed DISPLAY text from canonical SI
        _thicknessField.Seed(ToDisplayFromMm(ThicknessValue, UnitSystem));
        _coolingField.Seed(ToDisplayFromCPerSec(CoolingRateValue, UnitSystem));

        OnPropertyChanged(nameof(CarbonText));
        OnPropertyChanged(nameof(SiliconText));
        OnPropertyChanged(nameof(ManganeseText));
        OnPropertyChanged(nameof(PhosphorusText));
        OnPropertyChanged(nameof(SulfurText));
        OnPropertyChanged(nameof(ThicknessText));
        OnPropertyChanged(nameof(CoolingRateText));

        OnPropertyChanged(nameof(ThicknessValue));
        OnPropertyChanged(nameof(CoolingRateValue));

        OnPropertyChanged(nameof(IsValid));
        InvalidateCanExecute();
        UpdateCompositionPlot();
    }

    /// <summary>
    /// Determines whether the Calculate command can execute.
    /// </summary>
    /// <returns>
    /// <c>true</c> if all seven input fields contain valid values; otherwise, <c>false</c>.
    /// </returns>
    private bool CanCalculate()
        => _carbonField.IsValid
        && _siliconField.IsValid
        && _manganeseField.IsValid
        && _phosphorusField.IsValid
        && _sulfurField.IsValid
        && _thicknessField.IsValid
        && _coolingField.IsValid;

    /// <summary>
    /// Triggers CanExecuteChanged on the Calculate command to update UI enabled state.
    /// </summary>
    private void InvalidateCanExecute()
    {
        if (CalculateCommand is RelayCommand rc)
            rc.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Rebuilds unit-sensitive validation fields and re-seeds their text from canonical SI values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called when <see cref="UnitSystem"/> changes. It performs:
    /// <list type="number">
    ///   <item><description>Rebuilds thickness field with display-unit validation range and format</description></item>
    ///   <item><description>Rebuilds cooling rate field with display-unit validation range and format</description></item>
    ///   <item><description>Seeds both fields from canonical SI values converted to new display units</description></item>
    ///   <item><description>Raises property change notifications for affected properties</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Critical:</strong> Canonical SI values (<see cref="ThicknessValue"/>, <see cref="CoolingRateValue"/>)
    /// are never modified - only the display representation changes.
    /// </para>
    /// </remarks>
    private void RebuildUnitSensitiveFieldsAndReseed()
    {
        // Rebuild validators + formats to match what the user types (display units)
        _thicknessField = BuildThicknessField(_unitSystem);
        _coolingField = BuildCoolingField(_unitSystem);

        // Seed new fields from canonical SI values (converted to display)
        _thicknessField.Seed(ToDisplayFromMm(ThicknessValue, _unitSystem));
        _coolingField.Seed(ToDisplayFromCPerSec(CoolingRateValue, _unitSystem));

        // Notify bindings
        OnPropertyChanged(nameof(ThicknessText));
        OnPropertyChanged(nameof(CoolingRateText));
        OnPropertyChanged(nameof(ThicknessValue));
        OnPropertyChanged(nameof(CoolingRateValue));
        OnPropertyChanged(nameof(IsValid));
        InvalidateCanExecute();
    }

    /// <summary>
    /// Builds a thickness validation field configured for the specified unit system.
    /// </summary>
    /// <param name="units">The unit system determining validation range and format.</param>
    /// <returns>A configured <see cref="NumericTextField"/> that validates in display units.</returns>
    /// <remarks>
    /// For American Standard: validates in inches with range ~0.118-5.906 in (format: "0.###").
    /// For Standard: validates in millimeters with range 3-150 mm (format: "0.#").
    /// </remarks>
    private static NumericTextField BuildThicknessField(UnitSystem units)
    {
        // Validate in DISPLAY units, store canonical in mm
        return units == UnitSystem.AmericanStandard
            ? NumericTextField.Range(
                "Thickness",
                ThicknessMinMm / MmPerIn,
                ThicknessMaxMm / MmPerIn,
                ThicknessFormat_In)
            : NumericTextField.Range(
                "Thickness",
                ThicknessMinMm,
                ThicknessMaxMm,
                ThicknessFormat_Mm);
    }

    /// <summary>
    /// Builds a cooling rate validation field configured for the specified unit system.
    /// </summary>
    /// <param name="units">The unit system determining validation range and format.</param>
    /// <returns>A configured <see cref="NumericTextField"/> that validates in display units.</returns>
    /// <remarks>
    /// For American Standard: validates in °F/s with range ~0.018-3.6 °F/s (format: "0.####").
    /// For Standard: validates in °C/s with range 0.01-2.0 °C/s (format: "0.####").
    /// </remarks>
    private static NumericTextField BuildCoolingField(UnitSystem units)
    {
        // Validate in DISPLAY units, store canonical in °C/s
        return units == UnitSystem.AmericanStandard
            ? NumericTextField.Range(
                "Cooling rate",
                CoolingMinCPerSec * FPerC,
                CoolingMaxCPerSec * FPerC,
                CoolingFormat_FPerSec)
            : NumericTextField.Range(
                "Cooling rate",
                CoolingMinCPerSec,
                CoolingMaxCPerSec,
                CoolingFormat_CPerSec);
    }

    // ============================================================
    // Private Methods - Unit Conversions
    // ============================================================

    /// <summary>
    /// Converts a thickness value from display units to canonical millimeters.
    /// </summary>
    /// <param name="v">The thickness value in display units.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The thickness in millimeters (canonical SI).</returns>
    /// <remarks>
    /// For American Standard: v (inches) × 25.4 → mm.
    /// For Standard: v (mm) → mm (no conversion).
    /// </remarks>
    private static double ToMmFromDisplay(double v, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? v * MmPerIn : v;

    /// <summary>
    /// Converts a cooling rate value from display units to canonical °C/s.
    /// </summary>
    /// <param name="v">The cooling rate value in display units.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The cooling rate in °C/s (canonical SI).</returns>
    /// <remarks>
    /// For American Standard: v (°F/s) × (5/9) → °C/s.
    /// For Standard: v (°C/s) → °C/s (no conversion).
    /// </remarks>
    private static double ToCPerSecFromDisplay(double v, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? v * (5.0 / 9.0) : v;

    /// <summary>
    /// Converts a thickness value from canonical millimeters to display units.
    /// </summary>
    /// <param name="mm">The thickness value in millimeters (canonical SI).</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The thickness in display units (inches or millimeters).</returns>
    /// <remarks>
    /// For American Standard: mm ÷ 25.4 → inches.
    /// For Standard: mm → mm (no conversion).
    /// </remarks>
    private static double ToDisplayFromMm(double mm, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? mm / MmPerIn : mm;

    /// <summary>
    /// Converts a cooling rate value from canonical °C/s to display units.
    /// </summary>
    /// <param name="cPerSec">The cooling rate value in °C/s (canonical SI).</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The cooling rate in display units (°F/s or °C/s).</returns>
    /// <remarks>
    /// For American Standard: cPerSec × (9/5) → °F/s.
    /// For Standard: cPerSec → °C/s (no conversion).
    /// </remarks>
    private static double ToDisplayFromCPerSec(double cPerSec, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? cPerSec * FPerC : cPerSec;

    // ============================================================
    // Private Methods - Chart Management
    // ============================================================

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
    /// Updates the composition bar chart with current input values.
    /// </summary>
    /// <remarks>
    /// This method is called automatically when any composition property changes.
    /// Values are clamped to valid display ranges before display.
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
    /// <para>
    /// This method caches the last values to support theme changes without recalculation.
    /// Hardness values are normalized to a 0-1 scale for gauge display using a fixed
    /// window (140-320 HB).
    /// </para>
    /// </remarks>
    private void UpdateGaugeModels(bool hasResult, double graphScore01, int hbMin, int hbMax)
    {
        _lastHasResult = hasResult;
        _lastGraphScore01 = graphScore01;
        _lastHbMin = hbMin;
        _lastHbMax = hbMax;

        if (_graphGaugeModel is null || _graphGaugeSeries is null ||
            _hardnessGaugeModel is null || _hardnessGaugeSeries is null)
            return;

        UpdateDonut(
            model: _graphGaugeModel,
            series: _graphGaugeSeries,
            value01: hasResult ? Clamp01(graphScore01) : 0.0,
            fill: IsDarkTheme
                ? OxyColor.FromArgb(230, 80, 200, 120)
                : OxyColor.FromArgb(255, 30, 150, 80),
            isDarkTheme: IsDarkTheme);

        double hbMid = 0.0;
        if (hasResult && hbMax >= hbMin && hbMin > 0)
            hbMid = (hbMin + hbMax) / 2.0;

        var hbNorm = hasResult
            ? Clamp01((hbMid - HbMinWindow) / (HbMaxWindow - HbMinWindow))
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
    /// Builds a composition bar chart plot model with appropriate theming.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme colors.</param>
    /// <param name="compositionSeries">Outputs the bar series for composition data.</param>
    /// <returns>A configured <see cref="PlotModel"/> for the composition chart.</returns>
    private static PlotModel BuildCompositionModel(bool isDark, out BarSeries compositionSeries)
    {
        var model = NewThemedModel(isDark);

        var text = isDark ? OxyColor.FromRgb(230, 230, 230) : OxyColor.FromRgb(20, 20, 20);
        var axisLine = isDark ? OxyColor.FromArgb(120, 255, 255, 255) : OxyColor.FromArgb(200, 0, 0, 0);
        var grid = isDark ? OxyColor.FromArgb(30, 255, 255, 255) : OxyColor.FromArgb(70, 0, 0, 0);

        model.Axes.Add(new CategoryAxis
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
        });

        model.Axes.Add(new LinearAxis
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
        });

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

        model.Series.Add(compositionSeries);
        return model;
    }

    /// <summary>
    /// Builds a gauge (donut chart) plot model with appropriate theming.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme colors.</param>
    /// <param name="gaugeSeries">Outputs the pie series for gauge data.</param>
    /// <returns>A configured <see cref="PlotModel"/> for a gauge visualization.</returns>
    private static PlotModel BuildGaugeModel(bool isDark, out PieSeries gaugeSeries)
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
        => isDark
            ? new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBackground = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.Transparent,
                TextColor = OxyColor.FromRgb(230, 230, 230),
                DefaultFontSize = 12
            }
            : new PlotModel
            {
                Background = OxyColor.FromRgb(255, 255, 255),
                PlotAreaBackground = OxyColor.FromRgb(255, 255, 255),
                PlotAreaBorderColor = OxyColor.FromArgb(140, 0, 0, 0),
                TextColor = OxyColor.FromRgb(20, 20, 20),
                DefaultFontSize = 13
            };

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

    // ============================================================
    // Private Methods - Utilities
    // ============================================================

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

    // ============================================================
    // Private Methods - Property Change Notification
    // ============================================================

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="name">
    /// The name of the property that changed (automatically provided by the compiler via CallerMemberName).
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}