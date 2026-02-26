using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Validation;
using Castara.Domain.Estimation.Services;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
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

public sealed class CalculationsViewModel : INotifyPropertyChanged, IThemeAware, IDataErrorInfo
{
    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;

    public event PropertyChangedEventHandler? PropertyChanged;



    // ---------------------------------------
    // Numeric canonical values (domain uses these)
    // ---------------------------------------
    public double Carbon { get; private set; }
    public double Silicon { get; private set; }
    public double Manganese { get; private set; }
    public double Phosphorus { get; private set; }
    public double Sulfur { get; private set; }

    public double ThicknessMm { get; private set; }
    public double CoolingRateCPerSec { get; private set; }

    // ---------------------------------------
    // Tooltips (your XAML binds to these)
    // ---------------------------------------
    public string CarbonTooltip =>
        $"Carbon (C), wt%.\nValid range: {CastIronInputConstraints.CarbonMin:0.##} – {CastIronInputConstraints.CarbonMax:0.##}.";

    public string SiliconTooltip =>
        $"Silicon (Si), wt%.\nValid range: {CastIronInputConstraints.SiliconMin:0.##} – {CastIronInputConstraints.SiliconMax:0.##}.";

    public string ManganeseTooltip =>
        $"Manganese (Mn), wt%.\nValid range: {CastIronInputConstraints.ManganeseMin:0.##} – {CastIronInputConstraints.ManganeseMax:0.##}.";

    public string PhosphorusTooltip =>
        $"Phosphorus (P), wt%.\nValid range: {CastIronInputConstraints.PhosphorusMin:0.##} – {CastIronInputConstraints.PhosphorusMax:0.##}.";

    public string SulfurTooltip =>
        $"Sulfur (S), wt%.\nValid range: {CastIronInputConstraints.SulfurMin:0.##} – {CastIronInputConstraints.SulfurMax:0.##}.";

    public string ThicknessTooltip =>
        "Section thickness in millimeters.\nValid range: > 0 mm.";

    public string CoolingRateTooltip =>
        $"Cooling rate in °C/s (continuous).\nValid range: > 0 °C/s.\nTypical casting guidance: {CastIronInputConstraints.CoolingRateTypicalMin:0.##} – {CastIronInputConstraints.CoolingRateTypicalMax:0.##} °C/s.";

    // ---------------------------------------
    // Field helpers (raw text + validation)
    // ---------------------------------------
    private readonly NumericTextField _carbonField;
    private readonly NumericTextField _siliconField;
    private readonly NumericTextField _manganeseField;
    private readonly NumericTextField _phosphorusField;
    private readonly NumericTextField _sulfurField;
    private readonly NumericTextField _thicknessField;
    private readonly NumericTextField _coolingField;

    // For IDataErrorInfo: map property-name -> field
    private readonly Dictionary<string, NumericTextField> _fieldByProperty;

    public CalculationsViewModel(IStatusService status, ICastIronEstimator estimator)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));

        CalculateCommand = new RelayCommand(Calculate, CanCalculate);
        ClearCommand = new RelayCommand(Clear);

        // Define fields + rules once
        _carbonField = NumericTextField.Range("Carbon", CastIronInputConstraints.CarbonMin, CastIronInputConstraints.CarbonMax);
        _siliconField = NumericTextField.Range("Silicon", CastIronInputConstraints.SiliconMin, CastIronInputConstraints.SiliconMax);
        _manganeseField = NumericTextField.Range("Manganese", CastIronInputConstraints.ManganeseMin, CastIronInputConstraints.ManganeseMax);
        _phosphorusField = NumericTextField.Range("Phosphorus", CastIronInputConstraints.PhosphorusMin, CastIronInputConstraints.PhosphorusMax);
        _sulfurField = NumericTextField.Range("Sulfur", CastIronInputConstraints.SulfurMin, CastIronInputConstraints.SulfurMax);
        _thicknessField = NumericTextField.MinPositive("Thickness", CastIronInputConstraints.ThicknessMinMm);
        _coolingField = NumericTextField.Range("Cooling rate", CastIronInputConstraints.CoolingRateMinCPerSec, CastIronInputConstraints.CoolingRateTypicalMax);

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

        // Defaults
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;

        ThicknessMm = 12.0;
        CoolingRateCPerSec = 1.0;

        // Seed raw text from numerics so startup shows no errors
        SeedAllTextFromNumerics();

        // Build plot models + seed visuals
        RebuildPlotsForTheme();
        UpdateCompositionPlot();
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        _status.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // ---------------------------------------
    // XAML expects IsValid (you can remove binding if you prefer)
    // ---------------------------------------
    public bool IsValid => CanCalculate();

    // ---------------------------------------
    // String wrappers (bind TextBox.Text to these)
    // ---------------------------------------
    public string CarbonText
    {
        get => _carbonField.Text;
        set => SetFieldText(_carbonField, value, v => Carbon = v, nameof(CarbonText));
    }

    public string SiliconText
    {
        get => _siliconField.Text;
        set => SetFieldText(_siliconField, value, v => Silicon = v, nameof(SiliconText));
    }

    public string ManganeseText
    {
        get => _manganeseField.Text;
        set => SetFieldText(_manganeseField, value, v => Manganese = v, nameof(ManganeseText));
    }

    public string PhosphorusText
    {
        get => _phosphorusField.Text;
        set => SetFieldText(_phosphorusField, value, v => Phosphorus = v, nameof(PhosphorusText));
    }

    public string SulfurText
    {
        get => _sulfurField.Text;
        set => SetFieldText(_sulfurField, value, v => Sulfur = v, nameof(SulfurText));
    }

    public string ThicknessMmText
    {
        get => _thicknessField.Text;
        set => SetFieldText(_thicknessField, value, v => ThicknessMm = v, nameof(ThicknessMmText));
    }

    public string CoolingRateCPerSecText
    {
        get => _coolingField.Text;
        set => SetFieldText(_coolingField, value, v => CoolingRateCPerSec = v, nameof(CoolingRateCPerSecText));
    }

    private void SetFieldText(NumericTextField field, string? value, Action<double> assignIfValid, string propertyName)
    {
        field.Text = value ?? string.Empty;

        // Only update the canonical numeric value when the text parses and validates
        if (field.TryGetValidValue(out var v))
        {
            assignIfValid(v);

            // If the UI binds the numeric properties, notify them too.
            if (ReferenceEquals(field, _thicknessField))
                OnPropertyChanged(nameof(ThicknessMm));

            if (ReferenceEquals(field, _coolingField))
                OnPropertyChanged(nameof(CoolingRateCPerSec));

            // Composition plot responds to changes in composition inputs
            if (ReferenceEquals(field, _carbonField) ||
                ReferenceEquals(field, _siliconField) ||
                ReferenceEquals(field, _manganeseField) ||
                ReferenceEquals(field, _phosphorusField) ||
                ReferenceEquals(field, _sulfurField))
            {
                UpdateCompositionPlot();
            }
        }

        // Tell WPF "validation might have changed"
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(IsValid)); // XAML button binding (if used)

        // update command enabled state
        InvalidateCanExecute();
    }

    private void SeedAllTextFromNumerics()
    {
        _carbonField.Seed(Carbon);
        _siliconField.Seed(Silicon);
        _manganeseField.Seed(Manganese);
        _phosphorusField.Seed(Phosphorus);
        _sulfurField.Seed(Sulfur);
        _thicknessField.Seed(ThicknessMm);
        _coolingField.Seed(CoolingRateCPerSec);

        OnPropertyChanged(nameof(CarbonText));
        OnPropertyChanged(nameof(SiliconText));
        OnPropertyChanged(nameof(ManganeseText));
        OnPropertyChanged(nameof(PhosphorusText));
        OnPropertyChanged(nameof(SulfurText));
        OnPropertyChanged(nameof(ThicknessMmText));
        OnPropertyChanged(nameof(CoolingRateCPerSecText));

        OnPropertyChanged(nameof(ThicknessMm));
        OnPropertyChanged(nameof(CoolingRateCPerSec));

        OnPropertyChanged(nameof(IsValid));

        InvalidateCanExecute();
        UpdateCompositionPlot();
    }

    // ---------------------------------------
    // IDataErrorInfo
    // ---------------------------------------
    public string Error => string.Empty;

    public string this[string columnName]
        => _fieldByProperty.TryGetValue(columnName, out var field)
            ? field.Error
            : string.Empty;

    // ---------------------------------------
    // Result + derived outputs (XAML expects these)
    // ---------------------------------------
    private CastIronEstimate? _result;
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

    public string CarbonEquivalentText =>
        Result is null ? "—" : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    public string GraphitizationScoreText =>
        Result is null ? "—" : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    public string HardnessText =>
        Result is null ? "—" : Result.EstimatedHardness.ToString();

    public string CoolingFactorText =>
        Result is null ? "—" : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    public string ThicknessFactorText =>
        Result is null ? "—" : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    public IReadOnlyList<RiskFlag> Flags =>
        Result?.Flags ?? Array.Empty<RiskFlag>();

    // ---------------------------------------
    // Commands
    // ---------------------------------------
    public ICommand CalculateCommand { get; }
    public ICommand ClearCommand { get; }

    private bool CanCalculate()
        => _carbonField.IsValid
        && _siliconField.IsValid
        && _manganeseField.IsValid
        && _phosphorusField.IsValid
        && _sulfurField.IsValid
        && _thicknessField.IsValid
        && _coolingField.IsValid;

    private void InvalidateCanExecute()
    {
        if (CalculateCommand is RelayCommand rc)
            rc.RaiseCanExecuteChanged();
    }

    private void Calculate()
    {
        try
        {
            if (!CanCalculate())
            {
                _status.Set(AppStatusLevel.Warning, "Check inputs", "One or more fields are invalid.");
                return;
            }

            var inputs = new CastIronInputs(
                Composition: new CastIronComposition(Carbon, Silicon, Manganese, Phosphorus, Sulfur),
                Section: new SectionProfile(ThicknessMm, CoolingRateCPerSec));

            Result = _estimator.Estimate(inputs);

            if (Result is not null)
            {
                UpdateGaugeModels(
                    hasResult: true,
                    graphScore01: Result.GraphitizationScore,
                    hbMin: Result.EstimatedHardness.MinHB,
                    hbMax: Result.EstimatedHardness.MaxHB);
            }
            else
            {
                UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
            }

            _status.Set(AppStatusLevel.Ok, "Calculated", "OK");
        }
        catch (Exception ex)
        {
            Result = null;
            UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
            _status.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
        }
    }

    private void Clear()
    {
        Result = null;
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);

        // reset numerics to defaults
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
    // Theme
    // ---------------------------------------
    private bool _isDarkTheme = true;
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

    public void SetTheme(bool isDark) => IsDarkTheme = isDark;

    // ============================================================
    // OxyPlot Models (theme-aware)
    // ============================================================

    private PlotModel? _compositionPlotModel;
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

    // Remember last gauge values so we can repaint on theme changes
    private bool _lastHasResult;
    private double _lastGraphScore01;
    private int _lastHbMin;
    private int _lastHbMax;

    private void RebuildPlotsForTheme()
    {
        CompositionPlotModel = BuildCompositionModel(IsDarkTheme, out _compositionSeries);
        GraphGaugeModel = BuildGaugeModel(IsDarkTheme, out _graphGaugeSeries);
        HardnessGaugeModel = BuildGaugeModel(IsDarkTheme, out _hardnessGaugeSeries);
    }

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

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static double ClampTo(double v, double min, double max)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return min;
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    // ---------------------------------------
    // Boilerplate
    // ---------------------------------------
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ============================================================
    // Helper type: one place for parsing + validation
    // ============================================================
    private sealed class NumericTextField
    {
        private readonly string _label;
        private readonly Func<double, string?> _validateNumeric; // returns error or null
        private readonly string _format;

        private NumericTextField(string label, Func<double, string?> validateNumeric, string format = "0.###")
        {
            _label = label;
            _validateNumeric = validateNumeric;
            _format = format;
        }

        public string Text { get; set; } = string.Empty;

        public bool IsValid => string.IsNullOrEmpty(Error);

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

        public void Seed(double value)
            => Text = value.ToString(_format, CultureInfo.InvariantCulture);

        public static NumericTextField Range(string label, double min, double max)
            => new(label, v => (v < min || v > max) ? $"{label} must be between {min:0.##} and {max:0.##}." : null);

        public static NumericTextField MinPositive(string label, double min)
            => new(label, v =>
            {
                if (v <= 0) return $"{label} must be > 0.";
                if (v < min) return $"{label} must be >= {min:0.####}.";
                return null;
            });
    }
}