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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The view model for the calculations view, managing cast iron composition inputs,
/// estimation calculations, result visualizations, and real-time validation with unit system support.
/// </summary>
public sealed class CalculationsViewModel : INotifyPropertyChanged, IThemeAware, IUnitAware, ICastingProfileAware, IDataErrorInfo
{
    // ============================================================
    // Constants - Green Sand Gray Iron Defaults
    // ============================================================

    private const double ThicknessMinMm = 3.0;
    private const double ThicknessMaxMm = 150.0;

    private const double CoolingMinCPerSec = 0.01;
    private const double CoolingMaxCPerSec = 2.0;

    private const double MmPerIn = 25.4;
    private const double FPerC = 9.0 / 5.0;

    private const double HbMinWindow = 140.0;
    private const double HbMaxWindow = 320.0;

    private const string ThicknessFormat_Mm = "0.#";
    private const string ThicknessFormat_In = "0.###";
    private const string CoolingFormat_CPerSec = "0.####";
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

    private NumericTextField _thicknessField;
    private NumericTextField _coolingField;

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

    private bool _lastHasResult;
    private double _lastGraphScore01;
    private int _lastHbMin;
    private int _lastHbMax;

    private CastingProfileDefinition? _selectedCastingProfile;

    // ============================================================
    // Events
    // ============================================================

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ============================================================
    // Constructor
    // ============================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsViewModel"/> class.
    /// </summary>
    /// <param name="status">The status service for displaying application status updates.</param>
    /// <param name="estimator">The cast iron estimator service for performing calculations.</param>
    /// <param name="log">Optional logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="status"/> or <paramref name="estimator"/> is null.</exception>
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

        _carbonField = NumericTextField.Range("Carbon", CastIronInputConstraints.CarbonMin, CastIronInputConstraints.CarbonMax);
        _siliconField = NumericTextField.Range("Silicon", CastIronInputConstraints.SiliconMin, CastIronInputConstraints.SiliconMax);
        _manganeseField = NumericTextField.Range("Manganese", CastIronInputConstraints.ManganeseMin, CastIronInputConstraints.ManganeseMax);
        _phosphorusField = NumericTextField.Range("Phosphorus", CastIronInputConstraints.PhosphorusMin, CastIronInputConstraints.PhosphorusMax);
        _sulfurField = NumericTextField.Range("Sulfur", CastIronInputConstraints.SulfurMin, CastIronInputConstraints.SulfurMax);

        _thicknessField = BuildThicknessField(_unitSystem);
        _coolingField = BuildCoolingField(_unitSystem);

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
        SeedAllTextFromNumerics();

        RebuildPlotsForTheme();
        UpdateCompositionPlot();
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        _status.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile");
        _log.LogInformation("CalculationsViewModel initialized");
    }

    // ============================================================
    // Properties - Canonical Numeric Values (Always SI Units)
    // ============================================================

    /// <summary>
    /// Gets the carbon content in weight percent (wt%).
    /// </summary>
    public double Carbon { get; private set; }

    /// <summary>
    /// Gets the silicon content in weight percent (wt%).
    /// </summary>
    public double Silicon { get; private set; }

    /// <summary>
    /// Gets the manganese content in weight percent (wt%).
    /// </summary>
    public double Manganese { get; private set; }

    /// <summary>
    /// Gets the phosphorus content in weight percent (wt%).
    /// </summary>
    public double Phosphorus { get; private set; }

    /// <summary>
    /// Gets the sulfur content in weight percent (wt%).
    /// </summary>
    public double Sulfur { get; private set; }

    /// <summary>
    /// Gets the section thickness value in millimeters (mm). This is the canonical SI unit value.
    /// </summary>
    public double ThicknessValue { get; private set; }

    /// <summary>
    /// Gets the cooling rate value in degrees Celsius per second (°C/s). This is the canonical SI unit value.
    /// </summary>
    public double CoolingRateValue { get; private set; }

    // ============================================================
    // Properties - Tooltips and Labels (Unit-Aware)
    // ============================================================

    /// <summary>
    /// Gets the tooltip text for the carbon input field, including valid range information.
    /// </summary>
    public string CarbonTooltip =>
        SelectedCastingProfile is null
            ? $"Carbon (C), wt%.\nGlobal range: {CastIronInputConstraints.CarbonMin:0.##} – {CastIronInputConstraints.CarbonMax:0.##}."
            : $"Carbon (C), wt%.\nProfile range: {SelectedCastingProfile.CarbonMin:0.##} – {SelectedCastingProfile.CarbonMax:0.##}.\nProfile: {SelectedCastingProfile.DisplayName}.";

    /// <summary>
    /// Gets the tooltip text for the silicon input field, including valid range information.
    /// </summary>
    public string SiliconTooltip =>
        SelectedCastingProfile is null
            ? $"Silicon (Si), wt%.\nGlobal range: {CastIronInputConstraints.SiliconMin:0.##} – {CastIronInputConstraints.SiliconMax:0.##}."
            : $"Silicon (Si), wt%.\nProfile range: {SelectedCastingProfile.SiliconMin:0.##} – {SelectedCastingProfile.SiliconMax:0.##}.\nProfile: {SelectedCastingProfile.DisplayName}.";

    /// <summary>
    /// Gets the tooltip text for the manganese input field, including valid range information.
    /// </summary>
    public string ManganeseTooltip =>
        SelectedCastingProfile is null
            ? $"Manganese (Mn), wt%.\nGlobal range: {CastIronInputConstraints.ManganeseMin:0.##} – {CastIronInputConstraints.ManganeseMax:0.##}."
            : $"Manganese (Mn), wt%.\nProfile range: {SelectedCastingProfile.ManganeseMin:0.##} – {SelectedCastingProfile.ManganeseMax:0.##}.\nProfile: {SelectedCastingProfile.DisplayName}.";

    /// <summary>
    /// Gets the tooltip text for the phosphorus input field, including valid range information.
    /// </summary>
    public string PhosphorusTooltip =>
        SelectedCastingProfile is null
            ? $"Phosphorus (P), wt%.\nGlobal range: {CastIronInputConstraints.PhosphorusMin:0.##} – {CastIronInputConstraints.PhosphorusMax:0.##}."
            : $"Phosphorus (P), wt%.\nProfile range: {SelectedCastingProfile.PhosphorusMin:0.##} – {SelectedCastingProfile.PhosphorusMax:0.##}.\nProfile: {SelectedCastingProfile.DisplayName}.";

    /// <summary>
    /// Gets the tooltip text for the sulfur input field, including valid range information.
    /// </summary>
    public string SulfurTooltip =>
        SelectedCastingProfile is null
            ? $"Sulfur (S), wt%.\nGlobal range: {CastIronInputConstraints.SulfurMin:0.##} – {CastIronInputConstraints.SulfurMax:0.##}."
            : $"Sulfur (S), wt%.\nProfile range: {SelectedCastingProfile.SulfurMin:0.##} – {SelectedCastingProfile.SulfurMax:0.##}.\nProfile: {SelectedCastingProfile.DisplayName}.";

    /// <summary>
    /// Gets the tooltip text for the thickness input field, including valid range in the current unit system.
    /// </summary>
    public string ThicknessTooltip
        => UnitSystem == UnitSystem.AmericanStandard
            ? $"Section thickness in inches (in).\nGlobal range: {ThicknessMinMm / MmPerIn:0.###} – {ThicknessMaxMm / MmPerIn:0.###} in.\n(Green sand gray iron defaults.)"
            : $"Section thickness in millimeters (mm).\nGlobal range: {ThicknessMinMm:0.##} – {ThicknessMaxMm:0.##} mm.\n(Green sand gray iron defaults.)";

    /// <summary>
    /// Gets the tooltip text for the cooling rate input field, including valid range in the current unit system.
    /// </summary>
    public string CoolingRateTooltip
        => UnitSystem == UnitSystem.AmericanStandard
            ? $"Cooling rate in °F/s.\nGlobal range: {CoolingMinCPerSec * FPerC:0.###} – {CoolingMaxCPerSec * FPerC:0.###} °F/s.\n(Green sand gray iron defaults.)"
            : $"Cooling rate in °C/s.\nGlobal range: {CoolingMinCPerSec:0.###} – {CoolingMaxCPerSec:0.###} °C/s.\n(Green sand gray iron defaults.)";

    /// <summary>
    /// Gets the label text for the thickness field in the current unit system.
    /// </summary>
    public string ThicknessLabel => UnitSystem == UnitSystem.AmericanStandard ? "Thickness (in)" : "Thickness (mm)";

    /// <summary>
    /// Gets the label text for the cooling rate field in the current unit system.
    /// </summary>
    public string CoolingRateLabel => UnitSystem == UnitSystem.AmericanStandard ? "Cooling Rate (°F/s)" : "Cooling Rate (°C/s)";

    /// <summary>
    /// Gets the unit suffix for the cooling rate in the current unit system.
    /// </summary>
    public string CoolingRateUnitSuffix => UnitSystem == UnitSystem.AmericanStandard ? "°F/s" : "°C/s";

    // ============================================================
    // Properties - Validation
    // ============================================================

    /// <summary>
    /// Gets a value indicating whether all input fields contain valid values.
    /// </summary>
    public bool IsValid => AreInputsValid();

    /// <summary>
    /// Gets an error message indicating what is wrong with this object. Always returns empty string.
    /// </summary>
    string IDataErrorInfo.Error => string.Empty;

    /// <summary>
    /// Gets the error message for the property with the given name.
    /// </summary>
    /// <param name="columnName">The name of the property whose error message to get.</param>
    /// <returns>The error message for the property, or an empty string if the property is valid.</returns>
    string IDataErrorInfo.this[string columnName]
    {
        get
        {
            if (!_fieldAccessorByProperty.TryGetValue(columnName, out var getter))
                return string.Empty;

            var fieldError = getter().Error;
            if (!string.IsNullOrWhiteSpace(fieldError))
                return fieldError;

            return GetCastingProfileValidationError(columnName);
        }
    }

    /// <summary>
    /// Gets the validation error message for a property based on the selected casting profile constraints.
    /// </summary>
    /// <param name="columnName">The name of the property to validate.</param>
    /// <returns>An error message if the value is outside the profile's valid range; otherwise, an empty string.</returns>
    private string GetCastingProfileValidationError(string columnName)
    {
        var profile = SelectedCastingProfile;
        if (profile is null)
            return string.Empty;

        return columnName switch
        {
            nameof(CarbonText) when !profile.IsValidCarbon(Carbon)
                => $"Carbon must be between {profile.CarbonMin:0.##} and {profile.CarbonMax:0.##} for {profile.DisplayName}.",

            nameof(SiliconText) when !profile.IsValidSilicon(Silicon)
                => $"Silicon must be between {profile.SiliconMin:0.##} and {profile.SiliconMax:0.##} for {profile.DisplayName}.",

            nameof(ManganeseText) when !profile.IsValidManganese(Manganese)
                => $"Manganese must be between {profile.ManganeseMin:0.##} and {profile.ManganeseMax:0.##} for {profile.DisplayName}.",

            nameof(PhosphorusText) when !profile.IsValidPhosphorus(Phosphorus)
                => $"Phosphorus must be between {profile.PhosphorusMin:0.##} and {profile.PhosphorusMax:0.##} for {profile.DisplayName}.",

            nameof(SulfurText) when !profile.IsValidSulfur(Sulfur)
                => $"Sulfur must be between {profile.SulfurMin:0.##} and {profile.SulfurMax:0.##} for {profile.DisplayName}.",

            _ => string.Empty
        };
    }

    // ============================================================
    // Properties - Text Input Wrappers (Display Units)
    // ============================================================

    /// <summary>
    /// Gets or sets the carbon content as a text string for UI binding.
    /// </summary>
    public string CarbonText
    {
        get => _carbonField.Text;
        set => SetFieldText(_carbonField, value, v => Carbon = v, nameof(CarbonText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the silicon content as a text string for UI binding.
    /// </summary>
    public string SiliconText
    {
        get => _siliconField.Text;
        set => SetFieldText(_siliconField, value, v => Silicon = v, nameof(SiliconText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the manganese content as a text string for UI binding.
    /// </summary>
    public string ManganeseText
    {
        get => _manganeseField.Text;
        set => SetFieldText(_manganeseField, value, v => Manganese = v, nameof(ManganeseText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the phosphorus content as a text string for UI binding.
    /// </summary>
    public string PhosphorusText
    {
        get => _phosphorusField.Text;
        set => SetFieldText(_phosphorusField, value, v => Phosphorus = v, nameof(PhosphorusText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the sulfur content as a text string for UI binding.
    /// </summary>
    public string SulfurText
    {
        get => _sulfurField.Text;
        set => SetFieldText(_sulfurField, value, v => Sulfur = v, nameof(SulfurText), refreshComposition: true);
    }

    /// <summary>
    /// Gets or sets the section thickness as a text string for UI binding.
    /// The value is displayed in the current unit system (mm or inches).
    /// </summary>
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
    /// Gets or sets the cooling rate as a text string for UI binding.
    /// The value is displayed in the current unit system (°C/s or °F/s).
    /// </summary>
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
    /// Gets the cast iron estimation result, or null if no calculation has been performed.
    /// </summary>
    public CastIronEstimate? Result
    {
        get => _result;
        private set
        {
            if (ReferenceEquals(_result, value))
                return;

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
    /// Gets the formatted carbon equivalent value text, or "—" if no result is available.
    /// </summary>
    public string CarbonEquivalentText
        => Result is null ? "—" : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted graphitization score text, or "—" if no result is available.
    /// </summary>
    public string GraphitizationScoreText
        => Result is null ? "—" : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted hardness range text, or "—" if no result is available.
    /// </summary>
    public string HardnessText
        => Result is null ? "—" : Result.EstimatedHardness.ToString();

    /// <summary>
    /// Gets the formatted cooling factor text, or "—" if no result is available.
    /// </summary>
    public string CoolingFactorText
        => Result is null ? "—" : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the formatted thickness factor text, or "—" if no result is available.
    /// </summary>
    public string ThicknessFactorText
        => Result is null ? "—" : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the collection of risk flags from the estimation result, or an empty collection if no result is available.
    /// </summary>
    public IReadOnlyList<RiskFlag> Flags
        => Result?.Flags ?? Array.Empty<RiskFlag>();

    // ============================================================
    // Properties - Charts
    // ============================================================

    /// <summary>
    /// Gets the plot model for the composition bar chart displaying element weights.
    /// </summary>
    public PlotModel CompositionPlotModel
    {
        get => _compositionPlotModel!;
        private set
        {
            _compositionPlotModel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the plot model for the graphitization score gauge (donut chart).
    /// </summary>
    public PlotModel GraphGaugeModel
    {
        get => _graphGaugeModel!;
        private set
        {
            _graphGaugeModel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the plot model for the hardness gauge (donut chart).
    /// </summary>
    public PlotModel HardnessGaugeModel
    {
        get => _hardnessGaugeModel!;
        private set
        {
            _hardnessGaugeModel = value;
            OnPropertyChanged();
        }
    }

    // ============================================================
    // Properties - Theme (IThemeAware)
    // ============================================================

    /// <summary>
    /// Gets or sets a value indicating whether dark theme is enabled.
    /// Changing this property rebuilds all charts with the appropriate theme.
    /// </summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
                return;

            _isDarkTheme = value;
            OnPropertyChanged();

            RebuildPlotsForTheme();
            UpdateCompositionPlot();
            UpdateGaugeModels(_lastHasResult, _lastGraphScore01, _lastHbMin, _lastHbMax);
        }
    }

    /// <summary>
    /// Sets the theme for the view model.
    /// </summary>
    /// <param name="isDark">True to use dark theme; false to use light theme.</param>
    public void SetTheme(bool isDark) => IsDarkTheme = isDark;

    // ============================================================
    // Properties - Unit System (IUnitAware)
    // ============================================================

    /// <summary>
    /// Gets or sets the unit system for display (Standard SI or American Standard).
    /// Changing this property rebuilds unit-sensitive fields and updates all related UI properties.
    /// </summary>
    public UnitSystem UnitSystem
    {
        get => _unitSystem;
        set
        {
            if (_unitSystem == value)
                return;

            _unitSystem = value;
            OnPropertyChanged();

            RebuildUnitSensitiveFieldsAndReseed();

            OnPropertyChanged(nameof(ThicknessTooltip));
            OnPropertyChanged(nameof(CoolingRateTooltip));
            OnPropertyChanged(nameof(CoolingRateUnitSuffix));
            OnPropertyChanged(nameof(ThicknessLabel));
            OnPropertyChanged(nameof(CoolingRateLabel));
        }
    }

    // ============================================================
    // Properties - Casting Profile (ICastingProfileAware)
    // ============================================================

    /// <summary>
    /// Gets the currently selected casting profile, or null if no profile is selected.
    /// </summary>
    public CastingProfileDefinition? SelectedCastingProfile
    {
        get => _selectedCastingProfile;
        private set
        {
            if (Equals(_selectedCastingProfile, value))
                return;

            _selectedCastingProfile = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCastingProfileDisplayName));
            OnPropertyChanged(nameof(CanCalculateDisplay));
            OnPropertyChanged(nameof(IsValid));

            OnPropertyChanged(nameof(CarbonText));
            OnPropertyChanged(nameof(SiliconText));
            OnPropertyChanged(nameof(ManganeseText));
            OnPropertyChanged(nameof(PhosphorusText));
            OnPropertyChanged(nameof(SulfurText));

            OnPropertyChanged(nameof(CarbonTooltip));
            OnPropertyChanged(nameof(SiliconTooltip));
            OnPropertyChanged(nameof(ManganeseTooltip));
            OnPropertyChanged(nameof(PhosphorusTooltip));
            OnPropertyChanged(nameof(SulfurTooltip));

            InvalidateCanExecute();
        }
    }

    /// <summary>
    /// Gets the display name of the selected casting profile, or a placeholder message if no profile is selected.
    /// </summary>
    public string SelectedCastingProfileDisplayName
        => SelectedCastingProfile?.DisplayName ?? "Select a casting profile";

    /// <summary>
    /// Gets a value indicating whether a calculation can be performed.
    /// Convenience property for binding button enabled state directly.
    /// </summary>
    public bool CanCalculateDisplay => CanCalculate();

    /// <summary>
    /// Sets the casting profile and applies its default values.
    /// </summary>
    /// <param name="profile">The casting profile to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public void SetCastingProfile(CastingProfileDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        SelectedCastingProfile = profile;
        ApplyCastingProfile(profile);
    }

    /// <summary>
    /// Applies the specified casting profile's default values to the input fields.
    /// If inputs are valid, automatically triggers a calculation; otherwise, clears the result.
    /// </summary>
    /// <param name="profile">The casting profile to apply.</param>
    private void ApplyCastingProfile(CastingProfileDefinition profile)
    {
        ThicknessValue = profile.DefaultSectionThicknessMm;

        RebuildUnitSensitiveFieldsAndReseed();

        if (CanCalculate())
        {
            Calculate();
        }
        else
        {
            Result = null;
            UpdateGaugeModels(false, 0, 0, 0);
            _status.Set(AppStatusLevel.Ok, "Profile changed", profile.DisplayName);
        }
    }

    // ============================================================
    // Properties - Commands
    // ============================================================

    /// <summary>
    /// Gets the command to perform cast iron estimation calculations.
    /// </summary>
    public ICommand CalculateCommand { get; }

    /// <summary>
    /// Gets the command to clear all input fields and reset to default values.
    /// </summary>
    public ICommand ClearCommand { get; }

    // ============================================================
    // Private Methods - Calculation Logic
    // ============================================================

    /// <summary>
    /// Performs the cast iron estimation calculation using the current input values and selected profile.
    /// Updates the Result property and status service with the outcome.
    /// </summary>
    private void Calculate()
    {
        _log.LogInformation("Calculation requested");

        try
        {
            if (!AreInputsValid())
            {
                _status.Set(AppStatusLevel.Warning, "Check inputs", "One or more fields are invalid.");
                return;
            }

            if (SelectedCastingProfile is null)
            {
                _status.Set(AppStatusLevel.Warning, "No profile selected", "A casting profile must be selected before calculation.");
                return;
            }

            var inputs = new CastIronInputs(
                Composition: new CastIronComposition(Carbon, Silicon, Manganese, Phosphorus, Sulfur),
                Section: new SectionProfile(ThicknessValue, CoolingRateValue));

            Result = _estimator.Estimate(inputs, SelectedCastingProfile);

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
                Result = null;
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
    /// Clears the calculation result and resets all input fields to default values.
    /// </summary>
    private void Clear()
    {
        _log.LogInformation("Inputs cleared");

        Result = null;
        UpdateGaugeModels(false, 0, 0, 0);

        ApplyDefaultNumerics();
        SeedAllTextFromNumerics();

        if (SelectedCastingProfile is null)
        {
            _status.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile");
        }
        else
        {
            _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
        }
    }

    /// <summary>
    /// Applies default numeric values to all composition and section input fields.
    /// These defaults represent typical gray iron composition values.
    /// </summary>
    private void ApplyDefaultNumerics()
    {
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;

        ThicknessValue = 12.0;
        CoolingRateValue = 1.0;
    }

    // ============================================================
    // Private Methods - Field Management
    // ============================================================

    /// <summary>
    /// Updates a numeric text field and its associated backing value, then triggers property change notifications.
    /// </summary>
    /// <param name="field">The numeric text field to update.</param>
    /// <param name="value">The new text value.</param>
    /// <param name="assignIfValid">Action to assign the validated numeric value to the backing property.</param>
    /// <param name="propertyName">The name of the property being updated.</param>
    /// <param name="refreshComposition">Whether to refresh the composition plot after updating.</param>
    /// <param name="afterValid">Optional action to execute after successful validation.</param>
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
        OnPropertyChanged(nameof(CanCalculateDisplay));
        InvalidateCanExecute();
    }

    /// <summary>
    /// Seeds all text fields from their corresponding numeric values and triggers all necessary property change notifications.
    /// </summary>
    private void SeedAllTextFromNumerics()
    {
        _carbonField.Seed(Carbon);
        _siliconField.Seed(Silicon);
        _manganeseField.Seed(Manganese);
        _phosphorusField.Seed(Phosphorus);
        _sulfurField.Seed(Sulfur);

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
        OnPropertyChanged(nameof(CanCalculateDisplay));

        InvalidateCanExecute();
        UpdateCompositionPlot();
    }

    /// <summary>
    /// Checks whether all input fields contain valid values.
    /// </summary>
    /// <returns>True if all inputs are valid; otherwise, false.</returns>
    private bool AreInputsValid()
        => string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(CarbonText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(SiliconText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(ManganeseText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(PhosphorusText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(SulfurText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(ThicknessText)])
        && string.IsNullOrEmpty(((IDataErrorInfo)this)[nameof(CoolingRateText)]);

    /// <summary>
    /// Determines whether the Calculate command can execute.
    /// </summary>
    /// <returns>True if a casting profile is selected and all inputs are valid; otherwise, false.</returns>
    private bool CanCalculate()
        => SelectedCastingProfile is not null && AreInputsValid();

    /// <summary>
    /// Notifies the CalculateCommand that its CanExecute state may have changed.
    /// </summary>
    private void InvalidateCanExecute()
    {
        if (CalculateCommand is RelayCommand rc)
            rc.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Rebuilds unit-sensitive fields (thickness and cooling rate) for the current unit system and reseeds their values.
    /// </summary>
    private void RebuildUnitSensitiveFieldsAndReseed()
    {
        _thicknessField = BuildThicknessField(_unitSystem);
        _coolingField = BuildCoolingField(_unitSystem);

        _thicknessField.Seed(ToDisplayFromMm(ThicknessValue, _unitSystem));
        _coolingField.Seed(ToDisplayFromCPerSec(CoolingRateValue, _unitSystem));

        OnPropertyChanged(nameof(ThicknessText));
        OnPropertyChanged(nameof(CoolingRateText));
        OnPropertyChanged(nameof(ThicknessValue));
        OnPropertyChanged(nameof(CoolingRateValue));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(CanCalculateDisplay));

        InvalidateCanExecute();
    }

    /// <summary>
    /// Creates a numeric text field configured for thickness input in the specified unit system.
    /// </summary>
    /// <param name="units">The unit system to use (Standard or AmericanStandard).</param>
    /// <returns>A configured numeric text field for thickness input.</returns>
    private static NumericTextField BuildThicknessField(UnitSystem units)
    {
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
    /// Creates a numeric text field configured for cooling rate input in the specified unit system.
    /// </summary>
    /// <param name="units">The unit system to use (Standard or AmericanStandard).</param>
    /// <returns>A configured numeric text field for cooling rate input.</returns>
    private static NumericTextField BuildCoolingField(UnitSystem units)
    {
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
    /// Converts a display value to millimeters based on the unit system.
    /// </summary>
    /// <param name="v">The value in display units.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The value in millimeters.</returns>
    private static double ToMmFromDisplay(double v, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? v * MmPerIn : v;

    /// <summary>
    /// Converts a display value to degrees Celsius per second based on the unit system.
    /// </summary>
    /// <param name="v">The value in display units.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The value in degrees Celsius per second.</returns>
    private static double ToCPerSecFromDisplay(double v, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? v * (5.0 / 9.0) : v;

    /// <summary>
    /// Converts millimeters to display units based on the unit system.
    /// </summary>
    /// <param name="mm">The value in millimeters.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The value in display units (mm or inches).</returns>
    private static double ToDisplayFromMm(double mm, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? mm / MmPerIn : mm;

    /// <summary>
    /// Converts degrees Celsius per second to display units based on the unit system.
    /// </summary>
    /// <param name="cPerSec">The value in degrees Celsius per second.</param>
    /// <param name="u">The current unit system.</param>
    /// <returns>The value in display units (°C/s or °F/s).</returns>
    private static double ToDisplayFromCPerSec(double cPerSec, UnitSystem u)
        => u == UnitSystem.AmericanStandard ? cPerSec * FPerC : cPerSec;

    // ============================================================
    // Private Methods - Chart Management
    // ============================================================

    /// <summary>
    /// Rebuilds all plot models for the current theme.
    /// </summary>
    private void RebuildPlotsForTheme()
    {
        CompositionPlotModel = BuildCompositionModel(IsDarkTheme, out _compositionSeries);
        GraphGaugeModel = BuildGaugeModel(IsDarkTheme, out _graphGaugeSeries);
        HardnessGaugeModel = BuildGaugeModel(IsDarkTheme, out _hardnessGaugeSeries);
    }

    /// <summary>
    /// Updates the composition bar chart with current element values.
    /// </summary>
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
    /// Updates both gauge models (graphitization and hardness) with the latest result values.
    /// </summary>
    /// <param name="hasResult">Whether a valid result is available.</param>
    /// <param name="graphScore01">The graphitization score (0-1 normalized).</param>
    /// <param name="hbMin">The minimum hardness value in HB.</param>
    /// <param name="hbMax">The maximum hardness value in HB.</param>
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
    /// Builds a bar chart plot model for displaying composition data.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme styling.</param>
    /// <param name="compositionSeries">Outputs the bar series for the composition data.</param>
    /// <returns>A configured plot model for the composition chart.</returns>
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
    /// Builds a donut/gauge chart plot model for displaying normalized scalar values.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme styling.</param>
    /// <param name="gaugeSeries">Outputs the pie series for the gauge data.</param>
    /// <returns>A configured plot model for the gauge chart.</returns>
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
    /// Creates a new plot model with theme-appropriate colors and settings.
    /// </summary>
    /// <param name="isDark">Whether to use dark theme styling.</param>
    /// <returns>A configured plot model with theme-appropriate styling.</returns>
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
    /// Updates a donut/gauge chart with a normalized value.
    /// </summary>
    /// <param name="model">The plot model to update.</param>
    /// <param name="series">The pie series to update.</param>
    /// <param name="value01">The normalized value between 0 and 1.</param>
    /// <param name="fill">The fill color for the value portion.</param>
    /// <param name="isDarkTheme">Whether dark theme is active.</param>
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
    /// Clamps a value between 0 and 1.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    /// <summary>
    /// Clamps a value between specified minimum and maximum bounds.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value, or the minimum if the value is NaN or infinity.</returns>
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
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="name">The name of the property that changed. Automatically provided by the compiler when called from a property setter.</param>
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}