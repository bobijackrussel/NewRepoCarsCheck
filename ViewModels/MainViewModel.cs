using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private const string VehiclesKey = "vehicles";
        private const string ReservationsKey = "reservations";
        private const string FeedbackKey = "feedback";
        private const string ViolationsKey = "violations";
        private const string SettingsKey = "settings";

        private const string AdminReservationsKey = "admin_reservations";
        private const string AdminVehiclesKey = "admin_vehicles";
        private const string AdminLocationsKey = "admin_locations";
        private const string AdminDiscountsKey = "admin_discounts";

        private readonly INavigationService _appNavigationService;
        private readonly IUserSession _userSession;
        private readonly ILocalizationService _localizationService;
        private readonly Func<VehiclesViewModel> _vehiclesFactory;
        private readonly Func<ReservationsViewModel> _reservationsFactory;
        private readonly Func<FeedbackViewModel> _feedbackFactory;
        private readonly Func<ReportViolationViewModel> _reportFactory;
        private readonly Func<SettingsViewModel> _settingsFactory;
        private readonly Func<AdminReservationManagementViewModel> _adminReservationsFactory;
        private readonly Func<AdminVehicleManagementViewModel> _adminVehiclesFactory;
        private readonly Func<AdminLocationManagementViewModel> _adminLocationsFactory;
        private readonly Func<AdminDiscountManagementViewModel> _adminDiscountsFactory;
        private readonly NavigationStore _contentNavigationStore;
        private readonly INavigationService _contentNavigationService;
        private readonly RelayCommand _navigateCommand;

        private bool _isMenuOpen;
        private string _currentSectionKey = VehiclesKey;
        private bool _hasAdminAccess;
        private BaseViewModel? _activeSection;
        private VehiclesViewModel? _currentVehiclesSection;
        private VehicleDetailsViewModel? _activeVehicleDetailsSection;

        public MainViewModel(
            INavigationService navigationService,
            IUserSession userSession,
            ILocalizationService localizationService,
            Func<VehiclesViewModel> vehiclesFactory,
            Func<ReservationsViewModel> reservationsFactory,
            Func<FeedbackViewModel> feedbackFactory,
            Func<ReportViolationViewModel> reportFactory,
            Func<SettingsViewModel> settingsFactory,
            Func<AdminReservationManagementViewModel> adminReservationsFactory,
            Func<AdminVehicleManagementViewModel> adminVehiclesFactory,
            Func<AdminLocationManagementViewModel> adminLocationsFactory,
            Func<AdminDiscountManagementViewModel> adminDiscountsFactory,
            ILoggerFactory loggerFactory)
        {
            _appNavigationService = navigationService;
            _userSession = userSession;
            _localizationService = localizationService;
            _vehiclesFactory = vehiclesFactory;
            _reservationsFactory = reservationsFactory;
            _feedbackFactory = feedbackFactory;
            _reportFactory = reportFactory;
            _settingsFactory = settingsFactory;
            _adminReservationsFactory = adminReservationsFactory;
            _adminVehiclesFactory = adminVehiclesFactory;
            _adminLocationsFactory = adminLocationsFactory;
            _adminDiscountsFactory = adminDiscountsFactory;
            _contentNavigationStore = new NavigationStore();
            _contentNavigationService = new NavigationService(
                _contentNavigationStore,
                loggerFactory.CreateLogger<NavigationService>());

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
            _contentNavigationStore.CurrentViewModelChanged += OnContentViewModelChanged;

            PrimaryMenuItems = new ObservableCollection<MenuItemViewModel>();
            AdminMenuItems = new ObservableCollection<MenuItemViewModel>();

            _navigateCommand = new RelayCommand(
                parameter => NavigateByRoute(parameter as string),
                parameter => parameter is string key && CanNavigateToKey(key));
            NavigateCommand = _navigateCommand;
            ToggleMenuCommand = new RelayCommand(_ => ToggleMenu());
            LogoutCommand = new RelayCommand(_ => Logout());

            IsMenuOpen = true;

            UpdateAdminAccess();
            NavigateByRoute(_currentSectionKey);
        }

        public ObservableCollection<MenuItemViewModel> PrimaryMenuItems { get; }

        public ObservableCollection<MenuItemViewModel> AdminMenuItems { get; }

        public ICommand NavigateCommand { get; }

        public BaseViewModel? CurrentViewModel => _contentNavigationStore.CurrentViewModel;

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        public bool HasAdminAccess
        {
            get => _hasAdminAccess;
            private set
            {
                if (SetProperty(ref _hasAdminAccess, value))
                {
                    OnPropertyChanged(nameof(HasAdminItems));
                }
            }
        }

        public bool HasAdminItems => HasAdminAccess && AdminMenuItems.Count > 0;

        public ICommand ToggleMenuCommand { get; }
        public ICommand LogoutCommand { get; }

        public string WelcomeMessage
        {
            get
            {
                var user = _userSession.CurrentUser;
                if (user == null)
                {
                    return _localizationService.GetString("Main_WelcomeGuest");
                }

                var displayName = $"{user.FirstName} {user.LastName}".Trim();
                var template = _localizationService.GetString("Main_WelcomeUser");
                return string.IsNullOrWhiteSpace(template) || template == "Main_WelcomeUser"
                    ? $"Welcome, {displayName}".Trim()
                    : string.Format(CultureInfo.CurrentCulture, template, displayName);
            }
        }

        public override void Dispose()
        {
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
            _contentNavigationStore.CurrentViewModelChanged -= OnContentViewModelChanged;

            DetachCurrentSection();
            if (_contentNavigationStore.CurrentViewModel != null)
            {
                _contentNavigationStore.CurrentViewModel.Dispose();
                _contentNavigationStore.CurrentViewModel = null;
            }

            _currentVehiclesSection = null;
            _activeVehicleDetailsSection = null;

            base.Dispose();
        }

        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private void Logout()
        {
            _userSession.CurrentUser = null;
            _currentSectionKey = VehiclesKey;
            _contentNavigationStore.CurrentViewModel?.Dispose();
            _contentNavigationStore.CurrentViewModel = null;
            _appNavigationService.NavigateTo<LoginViewModel>();
        }

        private void NavigateByRoute(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            NavigateToKey(key);
        }

        private void NavigateToKey(string key)
        {
            var target = FindMenuItem(key);
            if (target != null)
            {
                NavigateTo(target);
            }
        }

        private bool CanNavigateToKey(string key)
        {
            if (IsAdminKey(key) && !HasAdminAccess)
            {
                return false;
            }

            return FindMenuItem(key) != null;
        }

        private MenuItemViewModel? FindMenuItem(string key)
        {
            return PrimaryMenuItems
                .Concat(AdminMenuItems)
                .FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private void NavigateTo(MenuItemViewModel menuItem)
        {
            if (menuItem is null)
            {
                return;
            }

            var newSection = menuItem.CreateSection();
            if (_contentNavigationService.Navigate(newSection, disposePrevious: false))
            {
                _currentSectionKey = menuItem.Key;
                UpdateActiveStates();
            }
        }

        private void OnContentViewModelChanged(object? sender, BaseViewModel? viewModel)
        {
            if (ReferenceEquals(_activeSection, viewModel))
            {
                return;
            }

            DetachCurrentSection();
            _activeSection = viewModel;
            AttachCurrentSection();
            OnPropertyChanged(nameof(CurrentViewModel));
        }

        private void AttachCurrentSection()
        {
            switch (_activeSection)
            {
                case VehiclesViewModel vehicles:
                    vehicles.SetNavigationService(_contentNavigationService);
                    _currentVehiclesSection = vehicles;
                    break;
                case VehicleDetailsViewModel details:
                    _activeVehicleDetailsSection = details;
                    break;
            }
        }

        private void DetachCurrentSection()
        {
            switch (_activeSection)
            {
                case VehiclesViewModel vehicles:
                    if (ReferenceEquals(_currentVehiclesSection, vehicles))
                    {
                        _currentVehiclesSection = null;
                    }

                    break;
                case VehicleDetailsViewModel details:
                    if (ReferenceEquals(_activeVehicleDetailsSection, details))
                    {
                        _activeVehicleDetailsSection = null;
                    }

                    break;
            }
        }

        private void UpdateAdminAccess()
        {
            var newAccess = _userSession.CurrentUser?.IsAdministrator ?? false;
            HasAdminAccess = newAccess;
            UpdateMenuItems();
        }

        private void UpdateMenuItems()
        {
            var primaryDefinitions = new[]
            {
                new MenuDefinition(VehiclesKey, "Navigation_Vehicles", "\uECAD", () => _vehiclesFactory()),
                new MenuDefinition(ReservationsKey, "Navigation_Reservations", "\uE8B1", () => _reservationsFactory()),
                new MenuDefinition(FeedbackKey, "Navigation_Feedback", "\uE90A", () => _feedbackFactory()),
                new MenuDefinition(ViolationsKey, "Navigation_Violations", "\uEA18", () => _reportFactory()),
                new MenuDefinition(SettingsKey, "Navigation_Settings", "\uE713", () => _settingsFactory())
            };

            SyncMenuCollection(PrimaryMenuItems, primaryDefinitions);

            if (HasAdminAccess)
            {
                var adminDefinitions = new[]
                {
                    new MenuDefinition(AdminReservationsKey, "Navigation_AdminReservations", "\uE8B7", () => _adminReservationsFactory()),
                    new MenuDefinition(AdminVehiclesKey, "Navigation_AdminVehicles", "\uECAD", () => _adminVehiclesFactory()),
                    new MenuDefinition(AdminLocationsKey, "Navigation_AdminLocations", "\uE707", () => _adminLocationsFactory()),
                    new MenuDefinition(AdminDiscountsKey, "Navigation_AdminDiscounts", "\uE1C3", () => _adminDiscountsFactory())
                };

                SyncMenuCollection(AdminMenuItems, adminDefinitions);
            }
            else if (AdminMenuItems.Count > 0)
            {
                AdminMenuItems.Clear();
            }

            if (!HasAdminAccess && IsAdminKey(_currentSectionKey))
            {
                if (PrimaryMenuItems.FirstOrDefault() is { } firstItem)
                {
                    NavigateTo(firstItem);
                }
            }
            else
            {
                UpdateActiveStates();
            }

            _navigateCommand.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(HasAdminItems));
        }

        private void SyncMenuCollection(ObservableCollection<MenuItemViewModel> target, IEnumerable<MenuDefinition> definitions)
        {
            var existingByKey = target.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
            var desiredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            foreach (var definition in definitions)
            {
                desiredKeys.Add(definition.Key);
                var title = _localizationService.GetString(definition.TitleResourceKey);

                if (existingByKey.TryGetValue(definition.Key, out var existing))
                {
                    existing.Title = title;

                    if (!ReferenceEquals(target.ElementAt(index), existing))
                    {
                        target.Remove(existing);
                        target.Insert(index, existing);
                    }
                }
                else
                {
                    var item = new MenuItemViewModel(
                        definition.Key,
                        title,
                        definition.IconGlyph,
                        definition.Factory);

                    target.Insert(index, item);
                }

                index++;
            }

            for (var i = target.Count - 1; i >= 0; i--)
            {
                if (!desiredKeys.Contains(target[i].Key))
                {
                    target.RemoveAt(i);
                }
            }
        }

        private void UpdateActiveStates()
        {
            foreach (var item in PrimaryMenuItems)
            {
                item.IsActive = string.Equals(item.Key, _currentSectionKey, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var item in AdminMenuItems)
            {
                item.IsActive = string.Equals(item.Key, _currentSectionKey, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateMenuItems();
            OnPropertyChanged(nameof(WelcomeMessage));
        }

        private void OnCurrentUserChanged(object? sender, Models.User? e)
        {
            UpdateAdminAccess();
            OnPropertyChanged(nameof(WelcomeMessage));
        }

        private static bool IsAdminKey(string key)
        {
            return key is AdminReservationsKey or AdminVehiclesKey or AdminLocationsKey or AdminDiscountsKey;
        }

        private readonly record struct MenuDefinition(
            string Key,
            string TitleResourceKey,
            string IconGlyph,
            Func<BaseViewModel> Factory);

        public sealed class MenuItemViewModel : BaseViewModel
        {
            private readonly Func<BaseViewModel> _sectionFactory;
            private string _title;
            private bool _isActive;

            public MenuItemViewModel(
                string key,
                string title,
                string iconGlyph,
                Func<BaseViewModel> sectionFactory)
            {
                Key = key;
                _title = title;
                IconGlyph = iconGlyph;
                _sectionFactory = sectionFactory;
            }

            public string Key { get; }

            public string Title
            {
                get => _title;
                set => SetProperty(ref _title, value);
            }

            public string IconGlyph { get; }

            public bool IsActive
            {
                get => _isActive;
                set => SetProperty(ref _isActive, value);
            }

            public BaseViewModel CreateSection() => _sectionFactory();
        }
    }
}
