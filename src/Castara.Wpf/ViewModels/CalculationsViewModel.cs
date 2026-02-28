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
/// estimation calculations, result visualizations, and real-time validation.
/// </summary>
/// <remarks>
/// <para>
/// This view model orchestrates the entire cast iron analysis workflow with advanced features:
/// <list type="bullet">
///   <item><description>Real-time validation using <see cref="IDataErrorInfo"/> with field-level error messages</description></item>
///   <item><description>Text-based input with parsing and numeric storage separation</description></item>
///   <item><description>Calculation execution with comprehensive error handling and logging</description></item>
///   <item><description>Result display with formatted text properties for UI binding</description></item>
///   <item><description>Chart visualization (composition bars, graphitization/hardness gauges)</description></item>
///   <item><description>Theme switching via <see cref="IThemeAware"/> for consistent dark/light mode</description></item>
///   <item><description>Diagnostic logging at multiple levels (Trace, Debug, Info, Warning, Error)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Validation Architecture:</strong> The view model uses a two-layer approach:
/// <list type="number">
///   <item><description>Text properties (e.g., <see cref="CarbonText"/>) store raw user input</description></item>
///   <item><description>Numeric properties (e.g., <see cref="Carbon"/>) store validated values</description></item>
///   <item><description><see cref="NumericTextField"/> helper performs parsing and range validation</description></item>
///   <item><description><see cref="IDataErrorInfo"/> exposes errors to WPF validation system</description></item>
/// </list>
/// This approach allows real-time feedback while maintaining clean domain model inputs.
/// </para>
/// <para>
/// <strong>Chart Management:</strong> OxyPlot charts are rebuilt on theme changes and updated
/// reactively as inputs change. Gauge values are cached to survive theme switches without
/// recalculation.
/// </para>
/// </remarks>
public sealed class CalculationsViewModel : INotifyPropertyChanged, IThemeAware, IDataErrorInfo
{
    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;
    private readonly ILogger<CalculationsViewModel> _log;

    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ---------------------------------------
    // Numeric Canonical Values (domain uses these)
    // ---------------------------------------

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
    /// Gets the validated section thickness in millimeters.
    /// </summary>
    /// <value>
    /// The section thickness. This value is only updated when <see cref="ThicknessMmText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double ThicknessMm { get; private set; }

    /// <summary>
    /// Gets the validated cooling rate in degrees Celsius per second.
    /// </summary>
    /// <value>
    /// The cooling rate. This value is only updated when <see cref="CoolingRateCPerSecText"/> parses
    /// successfully and passes validation.
    /// </value>
    public double CoolingRateCPerSec { get; private set; }

    // ---------------------------------------
    // Tooltips (XAML binds to these)
    // ---------------------------------------

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
    /// Gets the tooltip text for the thickness input field with validation requirements.
    /// </summary>
    public string ThicknessTooltip =>
        "Section thickness in millimeters.\nValid range: > 0 mm.";

    /// <summary>
    /// Gets the tooltip text for the cooling rate input field with validation range and typical guidance.
    /// </summary>
    public string CoolingRateTooltip =>
        $"Cooling rate in °C/s (continuous).\nValid range: > 0 °C/s.\nTypical casting guidance: {CastIronInputConstraints.CoolingRateMin:0.##} – {CastIronInputConstraints.CoolingRateMax:0.##} °C/s.";

    // ---------------------------------------
    // Field Helpers (raw text + validation)
    // ---------------------------------------

    private readonly NumericTextField _carbonField;
    private readonly NumericTextField _siliconField;
    private readonly NumericTextField _manganeseField;
    private readonly NumericTextField _phosphorusField;
    private readonly NumericTextField _sulfurField;
    private readonly NumericTextField _thicknessField;
    private readonly NumericTextField _coolingField;

    /// <summary>
    /// Maps property names to their corresponding validation fields for <see cref="IDataErrorInfo"/> support.
    /// </summary>
    private readonly Dictionary<string, NumericTextField> _fieldByProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsViewModel"/> class.
    /// </summary>
    /// <param name="status">The status service for displaying operation results.</param>
    /// <param name="estimator">The cast iron estimator service for performing calculations.</param>
    /// <param name="log">The logger for diagnostic telemetry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="status"/> or <paramref name="estimator"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor performs comprehensive initialization:
    /// <list type="number">
    ///   <item><description>Creates validation fields for all inputs with range constraints</description></item>
    ///   <item><description>Sets default values (typical Class 30 gray iron composition)</description></item>
    ///   <item><description>Seeds text fields from numeric defaults to show valid initial state</description></item>
    ///   <item><description>Builds OxyPlot chart models for current theme</description></item>
    ///   <item><description>Updates composition chart with default values</description></item>
    ///   <item><description>Initializes gauge charts to zero state</description></item>
    ///   <item><description>Sets initial status to "Ready"</description></item>
    ///   <item><description>Logs initialization with debug-level details</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If <paramref name="log"/> is null, a <see cref="NullLogger{T}"/> is used to prevent exceptions
    /// while allowing the view model to function without logging.
    /// </para>
    /// </remarks>
    public CalculationsViewModel(
        IStatusService status,
        ICastIronEstimator estimator,
        ILogger<CalculationsViewModel> log)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _log = log ?? NullLogger<CalculationsViewModel>.Instance;

        CalculateCommand = new RelayCommand(Calculate, CanCalculate);
        ClearCommand = new RelayCommand(Clear);

        // Define validation fields with range constraints from domain
        _carbonField = NumericTextField.Range(
            "Carbon",
            CastIronInputConstraints.CarbonMin,
            CastIronInputConstraints.CarbonMax);

        _siliconField = NumericTextField.Range(
            "Silicon",
            CastIronInputConstraints.SiliconMin,
            CastIronInputConstraints.SiliconMax);

        _manganeseField = NumericTextField.Range(
            "Manganese",
            CastIronInputConstraints.ManganeseMin,
            CastIronInputConstraints.ManganeseMax);

        _phosphorusField = NumericTextField.Range(
            "Phosphorus",
            CastIronInputConstraints.PhosphorusMin,
            CastIronInputConstraints.PhosphorusMax);

        _sulfurField = NumericTextField.Range(
            "Sulfur",
            CastIronInputConstraints.SulfurMin,
            CastIronInputConstraints.SulfurMax);

        _thicknessField = NumericTextField.MinPositive(
            "Thickness",
            CastIronInputConstraints.ThicknessMinMm);

        _coolingField = NumericTextField.Range(
            "Cooling rate",
            CastIronInputConstraints.CoolingRateMinCPerSec,
            CastIronInputConstraints.CoolingRateMax);

        _fieldByProperty = new()
        {
            { nameof(CarbonText), _carbonField },
            { nameof(SiliconText), _siliconField },
            { nameof(ManganeseText), _manganeseField },
            { nameof(PhosphorusText), _phosphorusField },
            { nameof(SulfurText), _sulfurField },
            { nameof(ThicknessMmText), _thicknessField },
            { nameof(CoolingRateCPerSecText), _coolingField },
        };

        // Set default composition values (typical Class 30 gray iron)
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;

        ThicknessMm = 12.0;
        CoolingRateCPerSec = 1.0;

        _log.LogInformation("CalculationsViewModel initialized");
        _log.LogDebug(
            "Default inputs: C={Carbon} Si={Silicon} Mn={Manganese} P={Phosphorus} S={Sulfur} Thickness={Thickness} Cooling={Cooling}",
            Carbon, Silicon, Manganese, Phosphorus, Sulfur, ThicknessMm, CoolingRateCPerSec);

        // Seed text fields from numeric defaults to show valid initial state
        SeedAllTextFromNumerics();

        // Build and populate charts
        RebuildPlotsForTheme();
        UpdateCompositionPlot();
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // ---------------------------------------
    // Validation State
    // ---------------------------------------

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

    // ---------------------------------------
    // Text Wrappers (bind TextBox.Text to these)
    // ---------------------------------------

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
        set => SetFieldText(_carbonField, value, v => Carbon = v, nameof(CarbonText));
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
        set => SetFieldText(_siliconField, value, v => Silicon = v, nameof(SiliconText));
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
        set => SetFieldText(_manganeseField, value, v => Manganese = v, nameof(ManganeseText));
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
        set => SetFieldText(_phosphorusField, value, v => Phosphorus = v, nameof(PhosphorusText));
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
        set => SetFieldText(_sulfurField, value, v => Sulfur = v, nameof(SulfurText));
    }

    /// <summary>
    /// Gets or sets the thickness input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="ThicknessMm"/> is updated.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string ThicknessMmText
    {
        get => _thicknessField.Text;
        set => SetFieldText(_thicknessField, value, v => ThicknessMm = v, nameof(ThicknessMmText));
    }

    /// <summary>
    /// Gets or sets the cooling rate input text for UI binding.
    /// </summary>
    /// <value>
    /// The raw text entered by the user. May contain invalid or unparseable values.
    /// </value>
    /// <remarks>
    /// When set, this property attempts to parse and validate the text. If successful,
    /// <see cref="CoolingRateCPerSec"/> is updated.
    /// Validation errors are exposed through <see cref="IDataErrorInfo"/>.
    /// </remarks>
    public string CoolingRateCPerSecText
    {
        get => _coolingField.Text;
        set => SetFieldText(_coolingField, value, v => CoolingRateCPerSec = v, nameof(CoolingRateCPerSecText));
    }

    /// <summary>
    /// Sets a field's text value, attempts parsing/validation, and updates the canonical numeric value if valid.
    /// </summary>
    /// <param name="field">The validation field to update.</param>
    /// <param name="value">The new text value from the UI.</param>
    /// <param name="assignIfValid">Action to update the canonical numeric property if validation succeeds.</param>
    /// <param name="propertyName">The property name for change notification.</param>
    /// <remarks>
    /// <para>
    /// This method coordinates the validation workflow:
    /// <list type="number">
    ///   <item><description>Updates the field's text (logged at Debug level)</description></item>
    ///   <item><description>Attempts to parse and validate the text</description></item>
    ///   <item><description>If valid: updates numeric property, refreshes charts, logs at Trace level</description></item>
    ///   <item><description>If invalid: logs validation error at Trace level only (to avoid spam)</description></item>
    ///   <item><description>Raises property change notifications for text property and IsValid</description></item>
    ///   <item><description>Updates Calculate command's CanExecute state</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void SetFieldText(
        NumericTextField field,
        string? value,
        Action<double> assignIfValid,
        string propertyName)
    {
        _log.LogDebug("Field {PropertyName} changed to '{Value}'", propertyName, value);

        field.Text = value ?? string.Empty;

        // Only update the canonical numeric value when the text parses and validates
        if (field.TryGetValidValue(out var v))
        {
            _log.LogTrace("Field {PropertyName} parsed successfully = {NumericValue}", propertyName, v);

            assignIfValid(v);

            // Notify dependent numeric properties
            if (ReferenceEquals(field, _thicknessField))
                OnPropertyChanged(nameof(ThicknessMm));

            if (ReferenceEquals(field, _coolingField))
                OnPropertyChanged(nameof(CoolingRateCPerSec));

            // Update composition chart for composition inputs
            if (ReferenceEquals(field, _carbonField) ||
                ReferenceEquals(field, _siliconField) ||
                ReferenceEquals(field, _manganeseField) ||
                ReferenceEquals(field, _phosphorusField) ||
                ReferenceEquals(field, _sulfurField))
            {
                UpdateCompositionPlot();
            }
        }
        else
        {
            // Only log invalid at Trace to avoid spam while typing
            if (!field.IsValid)
                _log.LogTrace("Field {PropertyName} invalid: {Error}", propertyName, field.Error);
        }

        // Notify WPF that validation might have changed
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(IsValid));

        // Update command enabled state
        InvalidateCanExecute();
    }

    /// <summary>
    /// Initializes all text fields from their corresponding numeric default values.
    /// </summary>
    /// <remarks>
    /// This method is called during construction to ensure the UI displays valid
    /// initial values without validation errors. It also triggers chart updates
    /// and command state refresh.
    /// </remarks>
    private void SeedAllTextFromNumerics()
    {
        _carbonField.Seed(Carbon);
        _siliconField.Seed(Silicon);
        _manganeseField.Seed(Manganese);
        _phosphorusField.Seed(Phosphorus);
        _sulfurField.Seed(Sulfur);
        _thicknessField.Seed(ThicknessMm);
        _coolingField.Seed(CoolingRateCPerSec);

        // Notify all text properties
        OnPropertyChanged(nameof(CarbonText));
        OnPropertyChanged(nameof(SiliconText));
        OnPropertyChanged(nameof(ManganeseText));
        OnPropertyChanged(nameof(PhosphorusText));
        OnPropertyChanged(nameof(SulfurText));
        OnPropertyChanged(nameof(ThicknessMmText));
        OnPropertyChanged(nameof(CoolingRateCPerSecText));

        // Notify numeric properties
        OnPropertyChanged(nameof(ThicknessMm));
        OnPropertyChanged(nameof(CoolingRateCPerSec));

        OnPropertyChanged(nameof(IsValid));

        InvalidateCanExecute();
        UpdateCompositionPlot();

        _log.LogDebug("Seeded text fields from numeric defaults");
    }

    // ---------------------------------------
    // IDataErrorInfo Implementation
    // ---------------------------------------

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
    /// in tooltips, borders, or other validation UI elements.
    /// </remarks>
    string IDataErrorInfo.this[string columnName]
        => _fieldByProperty.TryGetValue(columnName, out var field)
            ? field.Error
            : string.Empty;

    // ---------------------------------------
    // Result and Derived Outputs
    // ---------------------------------------

    private CastIronEstimate? _result;

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
    public string CarbonEquivalentText =>
        Result is null ? "—" : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted graphitization score text for display.
    /// </summary>
    /// <value>
    /// The graphitization score (0-1 scale) formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string GraphitizationScoreText =>
        Result is null ? "—" : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted hardness range text for display.
    /// </summary>
    /// <value>
    /// The hardness range in Brinell (e.g., "205-235 HB"), or "—" if no result is available.
    /// </value>
    public string HardnessText =>
        Result is null ? "—" : Result.EstimatedHardness.ToString();

    /// <summary>
    /// Gets the formatted cooling factor text for display.
    /// </summary>
    /// <value>
    /// The cooling factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string CoolingFactorText =>
        Result is null ? "—" : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted thickness factor text for display.
    /// </summary>
    /// <value>
    /// The thickness factor value formatted to three decimal places, or "—" if no result is available.
    /// </value>
    public string ThicknessFactorText =>
        Result is null ? "—" : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the collection of risk flags identified during estimation.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="RiskFlag"/> instances, or an empty list if no result is available.
    /// </value>
    public IReadOnlyList<RiskFlag> Flags =>
        Result?.Flags ?? Array.Empty<RiskFlag>();

    // ---------------------------------------
    // Commands
    // ---------------------------------------

    /// <summary>
    /// Gets the command to execute the cast iron property estimation.
    /// </summary>
    /// <remarks>
    /// This command validates all inputs, executes the estimation calculation,
    /// updates visualizations, logs diagnostic information, and displays appropriate status messages.
    /// The command's CanExecute updates automatically as validation state changes.
    /// </remarks>
    public ICommand CalculateCommand { get; }

    /// <summary>
    /// Gets the command to clear the current estimation result and reset inputs to defaults.
    /// </summary>
    /// <remarks>
    /// This command resets all numeric values to defaults, re-seeds text fields,
    /// clears gauge visualizations, and returns the status to "Ready" state.
    /// </remarks>
    public ICommand ClearCommand { get; }

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
    /// Executes the cast iron property estimation calculation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    ///   <item><description>Logs calculation request at Information level</description></item>
    ///   <item><description>Validates all inputs (logs Warning if invalid)</description></item>
    ///   <item><description>Logs input values at Debug level</description></item>
    ///   <item><description>Builds domain input models from validated values</description></item>
    ///   <item><description>Calls the estimation service</description></item>
    ///   <item><description>Logs results at Information level</description></item>
    ///   <item><description>Updates gauge visualizations with results</description></item>
    ///   <item><description>Displays success status</description></item>
    ///   <item><description>Handles exceptions with Error logging and status display</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void Calculate()
    {
        _log.LogInformation("Calculation requested");

        try
        {
            if (!CanCalculate())
            {
                _log.LogWarning("Calculation blocked due to invalid inputs");
                _status.Set(AppStatusLevel.Warning, "Check inputs", "One or more fields are invalid.");
                return;
            }

            _log.LogDebug(
                "Inputs: C={Carbon} Si={Silicon} Mn={Manganese} P={Phosphorus} S={Sulfur} Thickness={Thickness} Cooling={Cooling}",
                Carbon, Silicon, Manganese, Phosphorus, Sulfur, ThicknessMm, CoolingRateCPerSec);

            var inputs = new CastIronInputs(
                Composition: new CastIronComposition(Carbon, Silicon, Manganese, Phosphorus, Sulfur),
                Section: new SectionProfile(ThicknessMm, CoolingRateCPerSec));

            Result = _estimator.Estimate(inputs);

            if (Result is not null)
            {
                _log.LogInformation(
                    "Calculation succeeded CE={CE} Graph={Graph} HB={HBMin}-{HBMax}",
                    Result.CarbonEquivalent,
                    Result.GraphitizationScore,
                    Result.EstimatedHardness.MinHB,
                    Result.EstimatedHardness.MaxHB);

                UpdateGaugeModels(
                    hasResult: true,
                    graphScore01: Result.GraphitizationScore,
                    hbMin: Result.EstimatedHardness.MinHB,
                    hbMax: Result.EstimatedHardness.MaxHB);
            }
            else
            {
                _log.LogWarning("Estimator returned null result");
                UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
            }

            _status.Set(AppStatusLevel.Ok, "Calculated", "OK");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Calculation failed");

            Result = null;
            UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

            _status.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
        }
    }

    /// <summary>
    /// Clears the current estimation result and resets all inputs to default values.
    /// </summary>
    /// <remarks>
    /// This method logs the clear operation at Information level, resets all numeric
    /// properties to their default values (typical Class 30 gray iron), re-seeds the
    /// text fields, clears visualizations, and returns the status to "Ready" state.
    /// </remarks>
    private void Clear()
    {
        _log.LogInformation("Inputs cleared");

        Result = null;
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        // Reset numeric values to defaults
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;
        ThicknessMm = 12.0;
        CoolingRateCPerSec = 1.0;

        SeedAllTextFromNumerics();

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // ---------------------------------------
    // Theme Management (IThemeAware)
    // ---------------------------------------

    private bool _isDarkTheme = true;

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
    ///   <item><description>Theme change is logged at Debug level</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="ShellViewModel"/> calls <see cref="SetTheme"/> when the
    /// application theme changes to keep all visualizations synchronized.
    /// </para>
    /// </remarks>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value) return;
            _isDarkTheme = value;

            _log.LogDebug("Theme changed: DarkTheme={IsDarkTheme}", value);

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

    /// <summary>
    /// Series references that belong to the current PlotModels.
    /// </summary>
    private BarSeries? _compositionSeries;
    private PieSeries? _graphGaugeSeries;
    private PieSeries? _hardnessGaugeSeries;

    /// <summary>
    /// Cached gauge values to preserve on theme changes.
    /// </summary>
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
        _log.LogDebug("Rebuilding plot models for theme");

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
    /// Values are clamped to valid display ranges before display.
    /// Logged at Trace level with all composition values.
    /// </remarks>
    private void UpdateCompositionPlot()
    {
        if (_compositionPlotModel is null || _compositionSeries is null)
            return;

        _log.LogTrace(
            "Updating composition plot C={C} Si={Si} Mn={Mn} P={P} S={S}",
            Carbon, Silicon, Manganese, Phosphorus, Sulfur);

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
        // Cache values for theme repainting
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

    // ---------------------------------------
    // Property Change Notification
    // ---------------------------------------

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="name">
    /// The name of the property that changed (automatically provided by the compiler via CallerMemberName).
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ============================================================
    // Helper Type: NumericTextField
    // ============================================================

    /// <summary>
    /// Encapsulates text-based numeric input with parsing, formatting, and validation logic.
    /// </summary>
    /// <remarks>
    /// This helper class provides a reusable pattern for numeric text fields that:
    /// <list type="bullet">
    ///   <item><description>Store raw text separately from parsed numeric values</description></item>
    ///   <item><description>Validate parsed values against configurable rules</description></item>
    ///   <item><description>Generate user-friendly error messages for WPF validation</description></item>
    ///   <item><description>Support seeding from numeric values with consistent formatting</description></item>
    /// </list>
    /// This approach enables real-time validation feedback while the user types without
    /// disrupting the editing experience.
    /// </remarks>
    private sealed class NumericTextField
    {
        private readonly string _label;
        private readonly Func<double, string?> _validateNumeric; // returns error message or null
        private readonly string _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericTextField"/> class.
        /// </summary>
        /// <param name="label">The display label for the field (used in error messages).</param>
        /// <param name="validateNumeric">
        /// A function that validates the parsed numeric value, returning an error message or null if valid.
        /// </param>
        /// <param name="format">The format string for converting numeric values to text (default: "0.###").</param>
        private NumericTextField(string label, Func<double, string?> validateNumeric, string format = "0.###")
        {
            _label = label;
            _validateNumeric = validateNumeric;
            _format = format;
        }

        /// <summary>
        /// Gets or sets the raw text value entered by the user.
        /// </summary>
        /// <value>
        /// The text as entered by the user, which may be empty, unparseable, or invalid.
        /// </value>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the current text represents a valid value.
        /// </summary>
        /// <value>
        /// <c>true</c> if the text is non-empty, parseable, and passes validation; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => string.IsNullOrEmpty(Error);

        /// <summary>
        /// Gets the error message for the current text value.
        /// </summary>
        /// <value>
        /// An error message describing the validation failure, or an empty string if valid.
        /// </value>
        /// <remarks>
        /// Error messages are generated for:
        /// <list type="bullet">
        ///   <item><description>Empty or whitespace-only text ("X is required.")</description></item>
        ///   <item><description>Unparseable text ("X must be a number.")</description></item>
        ///   <item><description>Out-of-range numeric values (custom message from validator)</description></item>
        /// </list>
        /// </remarks>
        public string Error
        {
            get
            {
                var raw = Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    return $"{_label} is required.";

                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    return $"{_label} must be a number.";

                var numericError = _validateNumeric(v);
                return numericError ?? string.Empty;
            }
        }

        /// <summary>
        /// Attempts to parse and validate the current text, returning the numeric value if successful.
        /// </summary>
        /// <param name="value">
        /// When this method returns, contains the parsed and validated numeric value if successful;
        /// otherwise, contains the default value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the text was successfully parsed and validated; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetValidValue(out double value)
        {
            value = default;

            var raw = Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return false;

            if (!string.IsNullOrEmpty(_validateNumeric(v) ?? string.Empty)) return false;

            value = v;
            return true;
        }

        /// <summary>
        /// Seeds the text field from a numeric value using the configured format.
        /// </summary>
        /// <param name="value">The numeric value to format and store as text.</param>
        /// <remarks>
        /// This is used during initialization and reset operations to set valid initial text
        /// without triggering validation errors.
        /// </remarks>
        public void Seed(double value)
            => Text = value.ToString(_format, CultureInfo.InvariantCulture);

        /// <summary>
        /// Creates a numeric text field with range validation.
        /// </summary>
        /// <param name="label">The display label for the field.</param>
        /// <param name="min">The minimum allowable value (inclusive).</param>
        /// <param name="max">The maximum allowable value (inclusive).</param>
        /// <returns>A configured <see cref="NumericTextField"/> with range validation.</returns>
        public static NumericTextField Range(string label, double min, double max)
            => new(label, v => (v < min || v > max)
                ? $"{label} must be between {min:0.##} and {max:0.##}."
                : null);

        /// <summary>
        /// Creates a numeric text field with minimum positive value validation.
        /// </summary>
        /// <param name="label">The display label for the field.</param>
        /// <param name="min">The minimum allowable value (must be positive).</param>
        /// <returns>A configured <see cref="NumericTextField"/> with minimum positive validation.</returns>
        public static NumericTextField MinPositive(string label, double min)
            => new(label, v =>
            {
                if (v <= 0) return $"{label} must be > 0.";
                if (v < min) return $"{label} must be >= {min:0.####}.";
                return null;
            });
    }
}