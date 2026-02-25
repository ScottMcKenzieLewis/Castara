// ===============================
// Castara.Wpf.ViewModels
// CalculationsViewModel.cs
// ===============================

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

using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Castara.Wpf.ViewModels;

public sealed class CalculationsViewModel : INotifyPropertyChanged
{
    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;

    public event PropertyChangedEventHandler? PropertyChanged;

    // -----------------------------
    // Input ranges (single source of truth)
    // -----------------------------
    private const double CarbonMin = 0.0, CarbonMax = 5.0;
    private const double SiliconMin = 0.0, SiliconMax = 5.0;
    private const double ManganeseMin = 0.0, ManganeseMax = 3.0;
    private const double PhosphorusMin = 0.0, PhosphorusMax = 1.0;
    private const double SulfurMin = 0.0, SulfurMax = 1.0;

    private const double ThicknessMinMm = 0.0001; // > 0
    private const double CoolingRateMinCPerSec = 0.0001; // > 0

    // Optional "sanity guidance" (not hard validation), shown in tooltip
    private const double CoolingRateTypicalMin = 0.05;
    private const double CoolingRateTypicalMax = 20.0;

    public string CoolingFactorText => Result is null
    ? "—"
    : Result.CoolingFactor.ToString("0.000", CultureInfo.InvariantCulture);

    public string ThicknessFactorText => Result is null
        ? "—"
        : Result.ThicknessFactor.ToString("0.000", CultureInfo.InvariantCulture);

    public string CarbonTooltip =>
    $"Carbon (C), wt%.\nValid range: {CarbonMin:0.##} – {CarbonMax:0.##}.";

    public string SiliconTooltip =>
        $"Silicon (Si), wt%.\nValid range: {SiliconMin:0.##} – {SiliconMax:0.##}.";

    public string ManganeseTooltip =>
        $"Manganese (Mn), wt%.\nValid range: {ManganeseMin:0.##} – {ManganeseMax:0.##}.";

    public string PhosphorusTooltip =>
        $"Phosphorus (P), wt%.\nValid range: {PhosphorusMin:0.##} – {PhosphorusMax:0.##}.";

    public string SulfurTooltip =>
        $"Sulfur (S), wt%.\nValid range: {SulfurMin:0.##} – {SulfurMax:0.##}.";

    public string ThicknessTooltip =>
        $"Section thickness in millimeters.\nValid range: > 0 mm.";

    public string CoolingRateTooltip =>
        $"Cooling rate in °C/s (continuous).\nValid range: > 0 °C/s.\nTypical casting guidance: {CoolingRateTypicalMin:0.##} – {CoolingRateTypicalMax:0.##} °C/s.";

    public CalculationsViewModel(
        IStatusService status,
        ICastIronEstimator estimator)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));

        CalculateCommand = new RelayCommand(Calculate);
        ClearCommand = new RelayCommand(Clear);

        // Default to dark; ShellViewModel should call SetTheme(...) to keep plots in sync.
        _isDarkTheme = true;

        // Build plot models + series FIRST (so UpdateCompositionPlot can safely run from setters)
        RebuildPlotsForTheme();

        // Demo defaults (composition)
        _carbon = 3.40;
        _silicon = 2.10;
        _manganese = 0.55;
        _phosphorus = 0.05;
        _sulfur = 0.02;

        // Demo defaults (section)
        _thicknessMm = 12.0;
        _coolingRateCPerSec = 1.0; // Stage 2: continuous °C/s input

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

        _status.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Theme (for PlotModels)
    // -----------------------------
    private bool _isDarkTheme;

    /// <summary>
    /// Used ONLY for chart theming (OxyPlot does not bind to WPF theme resources automatically).
    /// ShellViewModel should call SetTheme(isDark) when the app theme changes.
    /// </summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (!Set(ref _isDarkTheme, value))
                return;

            RebuildPlotsForTheme();

            // Repaint based on current values.
            UpdateCompositionPlot();
            UpdateGaugeModels(_lastHasResult, _lastGraphScore01, _lastHbMin, _lastHbMax);
        }
    }

    public void SetTheme(bool isDark) => IsDarkTheme = isDark;

    // -----------------------------
    // Inputs: Composition (wt%)
    // -----------------------------
    private double _carbon;
    public double Carbon { get => _carbon; set { if (Set(ref _carbon, value)) UpdateCompositionPlot(); } }

    private double _silicon;
    public double Silicon { get => _silicon; set { if (Set(ref _silicon, value)) UpdateCompositionPlot(); } }

    private double _manganese;
    public double Manganese { get => _manganese; set { if (Set(ref _manganese, value)) UpdateCompositionPlot(); } }

    private double _phosphorus;
    public double Phosphorus { get => _phosphorus; set { if (Set(ref _phosphorus, value)) UpdateCompositionPlot(); } }

    private double _sulfur;
    public double Sulfur { get => _sulfur; set { if (Set(ref _sulfur, value)) UpdateCompositionPlot(); } }

    // -----------------------------
    // Inputs: Section (Stage 2)
    // -----------------------------
    private double _thicknessMm;
    public double ThicknessMm { get => _thicknessMm; set => Set(ref _thicknessMm, value); }

    // Stage 2: continuous cooling rate input in °C/s (no bucketing)
    private double _coolingRateCPerSec;
    public double CoolingRateCPerSec { get => _coolingRateCPerSec; set => Set(ref _coolingRateCPerSec, value); }

    // -----------------------------
    // Outputs
    // -----------------------------
    private CastIronEstimate? _result;
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

    public bool HasResult => Result is not null;

    public string CarbonEquivalentText => Result is null
        ? "—"
        : Result.CarbonEquivalent.ToString("0.000", CultureInfo.InvariantCulture);

    public string GraphitizationScoreText => Result is null
        ? "—"
        : Result.GraphitizationScore.ToString("0.000", CultureInfo.InvariantCulture);

    public string HardnessText => Result is null
        ? "—"
        : Result.EstimatedHardness.ToString();

    public IReadOnlyList<RiskFlag> Flags => Result?.Flags ?? Array.Empty<RiskFlag>();

    // -----------------------------
    // Commands
    // -----------------------------
    public ICommand CalculateCommand { get; }
    public ICommand ClearCommand { get; }

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

    private void Clear()
    {
        Result = null;
        UpdateGaugeModels(hasResult: false, graphScore01: 0, hbMin: 0, hbMax: 0);
        _status.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Domain Builders
    // -----------------------------
    private CastIronComposition BuildComposition()
        => new(
            Carbon: Carbon,
            Silicon: Silicon,
            Manganese: Manganese,
            Phosphorus: Phosphorus,
            Sulfur: Sulfur);

    private SectionProfile BuildSection()
        => new(
            ThicknessMm: ThicknessMm,
            CoolingRateCPerSec: CoolingRateCPerSec);

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
    private List<string> ValidateInputs()
    {
        var issues = new List<string>();
        if (Carbon is < CarbonMin or > CarbonMax) issues.Add($"Carbon must be between {CarbonMin:0.##} and {CarbonMax:0.##}.");
        if (Silicon is < SiliconMin or > SiliconMax) issues.Add($"Silicon must be between {SiliconMin:0.##} and {SiliconMax:0.##}.");
        if (Manganese is < ManganeseMin or > ManganeseMax) issues.Add($"Manganese must be between {ManganeseMin:0.##} and {ManganeseMax:0.##}.");
        if (Phosphorus is < PhosphorusMin or > PhosphorusMax) issues.Add($"Phosphorus must be between {PhosphorusMin:0.##} and {PhosphorusMax:0.##}.");
        if (Sulfur is < SulfurMin or > SulfurMax) issues.Add($"Sulfur must be between {SulfurMin:0.##} and {SulfurMax:0.##}.");
        if (ThicknessMm <= 0) issues.Add("Thickness must be greater than 0 mm.");
        if (CoolingRateCPerSec <= 0) issues.Add("Cooling rate must be greater than 0 (°C/s).");

        return issues;
    }

    private static bool IsFinite(double value)
        => !(double.IsNaN(value) || double.IsInfinity(value));

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

    // Remember the last gauge values so we can repaint when theme changes.
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

        // Strong contrast in light mode
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

            // Remove labels completely
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

        // Light mode: force a real white canvas and visible border.
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

        // Normalize HB midpoint into [0..1] using a stable window so the gauge is readable.
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

    // -----------------------------
    // INotifyPropertyChanged helpers
    // -----------------------------
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}