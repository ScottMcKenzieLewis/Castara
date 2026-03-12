using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Castara.Application.Abstractions.Repositories;
using Castara.Domain.Casting;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;

namespace Castara.Wpf.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly IThemeAware _themeAware;
    private readonly IUnitAware _unitAware;
    private readonly ICastingProfileAware _castingProfileAware;
    private readonly ICastingProfileRepository _castingProfileRepository;

    private bool _isDarkMode;
    private object? _currentViewModel;
    private UnitSystem? _unitSystem;

    private bool _isLoadingCastingProfiles;
    private string? _castingProfilesLoadError;
    private CastingProfileDefinition? _selectedCastingProfile;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SelectedCastingProfileDescriptor =>
    SelectedCastingProfile is null
        ? "No profile selected"
        : $"{SelectedCastingProfile.IronType} • {SelectedCastingProfile.ProcessFamily}";

    public ShellViewModel(
        IThemeService themeService,
        IStatusService statusService,
        IThemeAware themeAware,
        IUnitAware unitAware,
        ICastingProfileAware castingProfileAware,
        ICastingProfileRepository castingProfileRepository,
        LogViewerViewModel logViewer)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _themeAware = themeAware ?? throw new ArgumentNullException(nameof(themeAware));
        _unitAware = unitAware ?? throw new ArgumentNullException(nameof(unitAware));
        _castingProfileAware = castingProfileAware ?? throw new ArgumentNullException(nameof(castingProfileAware));
        _castingProfileRepository = castingProfileRepository ?? throw new ArgumentNullException(nameof(castingProfileRepository));
        LogViewerViewModel = logViewer ?? throw new ArgumentNullException(nameof(logViewer));

        CurrentViewModel = themeAware;

        _statusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IStatusService.Current))
            {
                Notify(nameof(StatusLeftText));
                Notify(nameof(StatusRightText));
                Notify(nameof(StatusBrush));
            }
        };

        UnitSystem = UnitSystem.Standard;
        IsDarkMode = true;

        _statusService.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    public LogViewerViewModel LogViewerViewModel { get; }

    public ObservableCollection<CastingProfileDefinition> CastingProfiles { get; } = [];

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (Equals(_currentViewModel, value))
                return;

            _currentViewModel = value;
            Notify(nameof(CurrentViewModel));
        }
    }

    public bool IsLoadingCastingProfiles
    {
        get => _isLoadingCastingProfiles;
        private set
        {
            if (_isLoadingCastingProfiles == value)
                return;

            _isLoadingCastingProfiles = value;
            Notify(nameof(IsLoadingCastingProfiles));
        }
    }

    public string? CastingProfilesLoadError
    {
        get => _castingProfilesLoadError;
        private set
        {
            if (_castingProfilesLoadError == value)
                return;

            _castingProfilesLoadError = value;
            Notify(nameof(CastingProfilesLoadError));
        }
    }

    public CastingProfileDefinition? SelectedCastingProfile
    {
        get => _selectedCastingProfile;
        set
        {
            if (Equals(_selectedCastingProfile, value))
                return;

            _selectedCastingProfile = value;
            Notify(nameof(SelectedCastingProfile));
            Notify(nameof(SelectedCastingProfileDisplayName));
            Notify(nameof(SelectedCastingProfileDescriptor));

            if (value is not null)
            {
                _castingProfileAware.SetCastingProfile(value);
                _statusService.Set(AppStatusLevel.Ok, "Profile", value.DisplayName);
            }
        }
    }

    public string SelectedCastingProfileDisplayName
        => SelectedCastingProfile?.DisplayName ?? "No profile selected";

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value)
                return;

            _isDarkMode = value;

            _themeService.SetDark(value);
            _themeAware.SetTheme(value);

            Notify(nameof(IsDarkMode));
        }
    }

    public UnitSystem UnitSystem
    {
        get => _unitSystem ?? UnitSystem.Standard;
        set
        {
            if (_unitSystem.HasValue && _unitSystem.Value == value)
                return;

            _unitSystem = value;
            _unitAware.UnitSystem = value;

            Notify(nameof(UnitSystem));
            Notify(nameof(IsAmericanStandard));
            Notify(nameof(UnitSystemLeftText));
            Notify(nameof(UnitSystemRightText));
        }
    }

    public bool IsAmericanStandard
    {
        get => UnitSystem == UnitSystem.AmericanStandard;
        set => UnitSystem = value ? UnitSystem.AmericanStandard : UnitSystem.Standard;
    }

    public string UnitSystemLeftText => "Units";
    public string UnitSystemRightText => IsAmericanStandard ? "American" : "Standard";

    public string StatusLeftText => _statusService.Current.LeftText;
    public string StatusRightText => _statusService.Current.RightText;

    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok =>
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35C759")),

            AppStatusLevel.Warning =>
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),

            AppStatusLevel.Error =>
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),

            _ => Brushes.Gray
        };

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (CastingProfiles.Count > 0)
            return;

        IsLoadingCastingProfiles = true;
        CastingProfilesLoadError = null;

        try
        {
            var profiles = await _castingProfileRepository.GetAllAsync(cancellationToken);

            CastingProfiles.Clear();

            foreach (var profile in profiles.OrderBy(p => p.DisplayName))
            {
                CastingProfiles.Add(profile);
            }

            SelectedCastingProfile = CastingProfiles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            CastingProfilesLoadError = ex.Message;
            _statusService.Set(AppStatusLevel.Error, "Profile load failed", ex.Message);
        }
        finally
        {
            IsLoadingCastingProfiles = false;
        }
    }

    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}