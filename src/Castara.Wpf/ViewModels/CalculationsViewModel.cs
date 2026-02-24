using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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

namespace Castara.Wpf.ViewModels;

public sealed class CalculationsViewModel : INotifyPropertyChanged
{
    private readonly IStatusService _status;
    private readonly ICastIronEstimator _estimator;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CalculationsViewModel(
        IStatusService status,
        ICastIronEstimator estimator)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));

        CalculateCommand = new RelayCommand(Calculate);
        ClearCommand = new RelayCommand(Clear);

        // Demo defaults (adjust as you like)
        Carbon = 3.40;
        Silicon = 2.10;
        Manganese = 0.55;
        Phosphorus = 0.05;
        Sulfur = 0.02;

        ThicknessMm = 12.0;
        CoolingRate = CoolingRate.Normal;

        _status.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Inputs: Composition (wt%)
    // -----------------------------

    private double _carbon;
    public double Carbon { get => _carbon; set => Set(ref _carbon, value); }

    private double _silicon;
    public double Silicon { get => _silicon; set => Set(ref _silicon, value); }

    private double _manganese;
    public double Manganese { get => _manganese; set => Set(ref _manganese, value); }

    private double _phosphorus;
    public double Phosphorus { get => _phosphorus; set => Set(ref _phosphorus, value); }

    private double _sulfur;
    public double Sulfur { get => _sulfur; set => Set(ref _sulfur, value); }

    // -----------------------------
    // Inputs: Section
    // -----------------------------

    private double _thicknessMm;
    public double ThicknessMm { get => _thicknessMm; set => Set(ref _thicknessMm, value); }

    private CoolingRate _coolingRate;
    public CoolingRate CoolingRate { get => _coolingRate; set => Set(ref _coolingRate, value); }

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
            _status.Set(AppStatusLevel.Warning, "Check inputs", issues[0]);
            return;
        }

        try
        {
            // Build domain inputs
            var composition = BuildComposition();
            var section = BuildSection();

            var inputs = new CastIronInputs(
                Composition: composition,
                Section: section);

            var estimate = _estimator.Estimate(inputs);

            Result = estimate;

            var right = estimate.Flags.Count == 0
                ? "No risks"
                : $"{estimate.Flags.Count} risk(s)";

            // If any High flags, elevate status to Warning (optional but feels “smart”)
            var level = AnyHighSeverity(estimate.Flags) ? AppStatusLevel.Warning : AppStatusLevel.Ok;

            _status.Set(level, "Calculated", right);
        }
        catch (Exception ex)
        {
            // Domain validation guards will throw — we surface as warning or error.
            Result = null;

            // If you prefer all thrown validation to be Warning, keep it Warning.
            _status.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
        }
    }

    private void Clear()
    {
        Result = null;
        _status.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Build Domain Inputs
    // -----------------------------

    private CastIronComposition BuildComposition()
    {
        // If CastIronComposition is a record with positional ctor, this will compile.
        // If it's settable properties instead, I can adjust in 10 seconds.
        return new CastIronComposition(
            Carbon: Carbon,
            Silicon: Silicon,
            Manganese: Manganese,
            Phosphorus: Phosphorus,
            Sulfur: Sulfur);
    }

    private SectionProfile BuildSection()
    {
        return new SectionProfile(
            ThicknessMm: ThicknessMm,
            CoolingRate: CoolingRate);
    }

    private static bool AnyHighSeverity(IReadOnlyList<RiskFlag> flags)
    {
        foreach (var f in flags)
            if (f.Severity == RiskSeverity.High)
                return true;

        return false;
    }

    // -----------------------------
    // Validation (UI-level, broad)
    // -----------------------------

    private List<string> ValidateInputs()
    {
        var issues = new List<string>();

        if (!IsFinite(Carbon) || !IsFinite(Silicon) || !IsFinite(Manganese) || !IsFinite(Phosphorus) || !IsFinite(Sulfur))
            issues.Add("All composition inputs must be valid numbers.");

        if (!IsFinite(ThicknessMm))
            issues.Add("Thickness must be a valid number.");

        if (ThicknessMm <= 0)
            issues.Add("Thickness must be greater than 0 mm.");

        // broad chemistry sanity checks
        if (Carbon is < 0 or > 5) issues.Add("Carbon must be between 0.00 and 5.00.");
        if (Silicon is < 0 or > 5) issues.Add("Silicon must be between 0.00 and 5.00.");
        if (Manganese is < 0 or > 3) issues.Add("Manganese must be between 0.00 and 3.00.");
        if (Phosphorus is < 0 or > 1) issues.Add("Phosphorus must be between 0.00 and 1.00.");
        if (Sulfur is < 0 or > 1) issues.Add("Sulfur must be between 0.00 and 1.00.");

        // CoolingRate is an enum, so only invalid if it's out of range (rare)
        if (!Enum.IsDefined(typeof(CoolingRate), CoolingRate))
            issues.Add("Cooling rate is invalid.");

        return issues;
    }

    private static bool IsFinite(double value)
        => !(double.IsNaN(value) || double.IsInfinity(value));

    // -----------------------------
    // INotifyPropertyChanged helpers
    // -----------------------------

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}