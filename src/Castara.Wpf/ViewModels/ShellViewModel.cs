using Castara.Application.Abstractions.Repositories;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Exceptions;
using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// View model for the main application shell, managing theme settings, unit system selection,
/// casting profile loading, and overall application state coordination.
/// </summary>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly IThemeAware _themeAware;
    private readonly IUnitAware _unitAware;
    private readonly ICastingProfileAware _castingProfileAware;
    private readonly ICastingProfileRepository _castingProfileRepository;
    private readonly IApplicationStateSnapshotService _applicationStateSnapshotService;
    private readonly ILogger<ShellViewModel> _logger;

#if DEBUG
    /// <summary>
    /// Gets the command to perform cast iron estimation calculations.
    /// </summary>
    public ICommand CrashTestCommand { get; }
#endif

    private bool _isDarkMode;
    private object? _currentViewModel;
    private UnitSystem? _unitSystem;

    private bool _isLoadingCastingProfiles;
    private string? _castingProfilesLoadError;
    private CastingProfileDefinition? _selectedCastingProfile;
    private CastingProfileOption? _selectedCastingProfileOption;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class.
    /// </summary>
    /// <param name="themeService">The theme service for managing application theme.</param>
    /// <param name="statusService">The status service for displaying application status.</param>
    /// <param name="themeAware">The theme-aware component (typically the main view model).</param>
    /// <param name="unitAware">The unit-aware component for handling unit system changes.</param>
    /// <param name="castingProfileAware">The component that responds to casting profile changes.</param>
    /// <param name="castingProfileRepository">The repository for loading casting profiles.</param>
    /// <param name="logViewer">The log viewer view model for displaying application logs.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public ShellViewModel(
        IThemeService themeService,
        IStatusService statusService,
        IThemeAware themeAware,
        IUnitAware unitAware,
        ICastingProfileAware castingProfileAware,
        ICastingProfileRepository castingProfileRepository,
        IApplicationStateSnapshotService applicationStateSnapshotService,
        LogViewerViewModel logViewer,
        ILogger<ShellViewModel> logger)
    {
        // Validate and store dependencies
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _themeAware = themeAware ?? throw new ArgumentNullException(nameof(themeAware));
        _unitAware = unitAware ?? throw new ArgumentNullException(nameof(unitAware));
        _castingProfileAware = castingProfileAware ?? throw new ArgumentNullException(nameof(castingProfileAware));
        _castingProfileRepository = castingProfileRepository ?? throw new ArgumentNullException(nameof(castingProfileRepository));
        _applicationStateSnapshotService = applicationStateSnapshotService ?? throw new ArgumentNullException(nameof(applicationStateSnapshotService));
        LogViewerViewModel = logViewer ?? throw new ArgumentNullException(nameof(logViewer));
        _logger = logger ?? NullLogger<ShellViewModel>.Instance;

        // Set the theme-aware component as the primary content view model
        CurrentViewModel = themeAware;

        // Subscribe to status service changes to update status display properties
        _statusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IStatusService.Current))
            {
                // Notify UI of status-related property changes
                Notify(nameof(StatusLeftText));
                Notify(nameof(StatusRightText));
                Notify(nameof(StatusBrush));
            }
        };

        // Initialize default settings
        UnitSystem = UnitSystem.Standard;
        IsDarkMode = true;

        // Add placeholder option to casting profile dropdown
        CastingProfileOptions.Add(new CastingProfileOption
        {
            DisplayName = "Select a casting profile",
            Profile = null
        });

        // Set the placeholder as the initial selection
        SelectedCastingProfileOption = CastingProfileOptions[0];

        _applicationStateSnapshotService.SetValue(ApplicationStateKeys.ActiveView, CurrentViewModel.GetType().Name);
        _applicationStateSnapshotService.SetValue(ApplicationStateKeys.Theme, IsDarkMode ? "Dark" : "Light");
        _applicationStateSnapshotService.SetValue(ApplicationStateKeys.UnitSystem, UnitSystem.ToString());

        // Display initial status message
        _statusService.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile");

#if DEBUG
        CrashTestCommand = new RelayCommand(() =>
        {
            throw new InvalidOperationException("Intentional crash test.");
        });
#endif

    }

    /// <summary>
    /// Gets the log viewer view model for displaying application diagnostic logs.
    /// </summary>
    public LogViewerViewModel LogViewerViewModel { get; }

    /// <summary>
    /// Gets the collection of available casting profiles.
    /// </summary>
    public ObservableCollection<CastingProfileDefinition> CastingProfiles { get; } = [];

    /// <summary>
    /// Gets the collection of casting profile options for UI binding, including a placeholder option.
    /// </summary>
    public ObservableCollection<CastingProfileOption> CastingProfileOptions { get; } = [];

    /// <summary>
    /// Gets the current active view model displayed in the shell's content area.
    /// </summary>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (Equals(_currentViewModel, value))
                return;

            _currentViewModel = value;
            _applicationStateSnapshotService.SetValue(ApplicationStateKeys.ActiveView, value?.GetType().Name);
            Notify(nameof(CurrentViewModel));
        }
    }

    /// <summary>
    /// Gets a value indicating whether casting profiles are currently being loaded from the repository.
    /// </summary>
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

    /// <summary>
    /// Gets the error message if casting profiles failed to load, or null if loading was successful.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the selected casting profile option from the dropdown.
    /// Setting this property updates the <see cref="SelectedCastingProfile"/>.
    /// </summary>
    public CastingProfileOption? SelectedCastingProfileOption
    {
        get => _selectedCastingProfileOption;
        set
        {
            if (Equals(_selectedCastingProfileOption, value))
                return;

            _selectedCastingProfileOption = value;
            SelectedCastingProfile = value?.Profile;

            Notify(nameof(SelectedCastingProfileOption));
        }
    }

    /// <summary>
    /// Gets or sets the currently selected casting profile.
    /// Setting this property notifies the casting profile aware component and updates the status service.
    /// </summary>
    public CastingProfileDefinition? SelectedCastingProfile
    {
        get => _selectedCastingProfile;
        set
        {
            if (Equals(_selectedCastingProfile, value))
                return;

            _selectedCastingProfile = value;

            _applicationStateSnapshotService.SetValue(ApplicationStateKeys.CastingProfile,
                value?.DisplayName);

            // Notify dependent properties that derive from the selected profile
            Notify(nameof(SelectedCastingProfile));
            Notify(nameof(SelectedCastingProfileDisplayName));
            Notify(nameof(SelectedCastingProfileDescriptor));

            if (value is null)
            {
                // No profile selected - prompt user to select one
                _statusService.Set(AppStatusLevel.Ok, "Ready", "Select a casting profile");
            }
            else
            {
                // Profile selected - apply it to the calculations view model and update status
                _castingProfileAware.SetCastingProfile(value);
                _statusService.Set(AppStatusLevel.Ok, "Profile", value.DisplayName);
            }
        }
    }

    /// <summary>
    /// Gets the display name of the selected casting profile, or a placeholder message if no profile is selected.
    /// </summary>
    public string SelectedCastingProfileDisplayName
        => SelectedCastingProfile?.DisplayName ?? "Select a casting profile";

    /// <summary>
    /// Gets a descriptive text for the selected casting profile, showing iron type and process family,
    /// or instructions if no profile is selected.
    /// </summary>
    public string SelectedCastingProfileDescriptor =>
        SelectedCastingProfile is null
            ? "Choose a casting profile to load defaults and estimation assumptions."
            : $"{SelectedCastingProfile.IronType} • {SelectedCastingProfile.ProcessFamily}";

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// Setting this property updates the theme service and notifies theme-aware components.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value)
                return;

            _isDarkMode = value;

            // Apply theme to application resources and visual elements
            _themeService.SetDark(value);

            // Notify theme-aware view models (e.g., CalculationsViewModel for chart colors)
            _themeAware.SetTheme(value);

            Notify(nameof(IsDarkMode));
        }
    }

    /// <summary>
    /// Gets or sets the current unit system (Standard SI or American Standard).
    /// Setting this property updates unit-aware components and related display properties.
    /// </summary>
    public UnitSystem UnitSystem
    {
        get => _unitSystem ?? UnitSystem.Standard;
        set
        {
            if (_unitSystem.HasValue && _unitSystem.Value == value)
                return;

            _unitSystem = value;
            _applicationStateSnapshotService.SetValue(ApplicationStateKeys.UnitSystem, _unitSystem.ToString());

            // Propagate unit system change to view models that display unit-sensitive values
            _unitAware.UnitSystem = value;

            // Notify all properties that depend on the unit system
            Notify(nameof(UnitSystem));
            Notify(nameof(IsAmericanStandard));
            Notify(nameof(UnitSystemLeftText));
            Notify(nameof(UnitSystemRightText));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the American Standard unit system is selected.
    /// This is a convenience property that maps to <see cref="UnitSystem"/>.
    /// </summary>
    public bool IsAmericanStandard
    {
        get => UnitSystem == UnitSystem.AmericanStandard;
        set => UnitSystem = value ? UnitSystem.AmericanStandard : UnitSystem.Standard;
    }

    /// <summary>
    /// Gets the label text for the unit system setting (always "Units").
    /// </summary>
    public string UnitSystemLeftText => "Units";

    /// <summary>
    /// Gets the current unit system display text ("American" or "Standard").
    /// </summary>
    public string UnitSystemRightText => IsAmericanStandard ? "American" : "Standard";

    /// <summary>
    /// Gets the left text from the current status state.
    /// </summary>
    public string StatusLeftText => _statusService.Current.LeftText;

    /// <summary>
    /// Gets the right text from the current status state.
    /// </summary>
    public string StatusRightText => _statusService.Current.RightText;

    /// <summary>
    /// Gets the brush color for the status indicator based on the current status level.
    /// </summary>
    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35C759")),
            AppStatusLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
            AppStatusLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),
            _ => Brushes.Gray
        };

    /// <summary>
    /// Initializes the shell by loading casting profiles from the repository.
    /// This method is idempotent and will only load profiles once.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Ensure idempotency - only load profiles once
        if (CastingProfiles.Count > 0)
            return;

        // Set loading state for UI feedback
        IsLoadingCastingProfiles = true;
        CastingProfilesLoadError = null;

        try
        {
            // Load profiles from repository (typically from embedded JSON resource)
            var profiles = await _castingProfileRepository.GetAllAsync(cancellationToken);

            // Clear any existing profiles (shouldn't happen due to idempotency check)
            CastingProfiles.Clear();
            CastingProfileOptions.Clear();

            // Add placeholder option first
            CastingProfileOptions.Add(new CastingProfileOption
            {
                DisplayName = "Select a casting profile",
                Profile = null
            });

            // Add all loaded profiles to both collections, sorted alphabetically
            foreach (var profile in profiles.OrderBy(p => p.DisplayName))
            {
                CastingProfiles.Add(profile);
                CastingProfileOptions.Add(new CastingProfileOption
                {
                    DisplayName = profile.DisplayName,
                    Profile = profile
                });
            }

            // Reset selection to placeholder
            SelectedCastingProfileOption = CastingProfileOptions[0];
        }
        catch (Exception ex)
        {
            // Log the error for diagnostics
            _logger.LogError(ex, "Failed to load casting profiles");

            // Store error message for UI display
            CastingProfilesLoadError = ex.Message;

            // Update status with user-friendly error message
            // Show domain-specific error messages when available, otherwise generic error
            _statusService.Set(
                AppStatusLevel.Error,
                "Profile load failed",
                ex is DomainException ? ex.Message : "Unexpected error occurred");
        }
        finally
        {
            // Clear loading state regardless of success or failure
            IsLoadingCastingProfiles = false;
        }
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
        return false;
#endif
        }
    }

}