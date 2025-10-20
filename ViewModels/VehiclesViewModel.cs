using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class VehiclesViewModel : SectionViewModel
    {
        private readonly IUserSession _userSession;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<VehiclesViewModel> _logger;
        private readonly RelayCommand _addVehicleCommand;
        private readonly RelayCommand _backToListCommand;
        private bool _isInitialized;
        private bool _showingDetails;
        private INavigationService? _navigationService;

        public VehiclesViewModel(
            VehicleListViewModel vehicleList,
            VehicleDetailsViewModel vehicleDetails,
            IUserSession userSession,
            ILocalizationService localizationService,
            ILogger<VehiclesViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            VehicleList = vehicleList;
            VehicleDetails = vehicleDetails;
            _userSession = userSession;
            _localizationService = localizationService;
            _logger = logger;

            VehicleList.SelectedVehicleChanged += OnSelectedVehicleChanged;
            VehicleDetails.ReserveRequested += OnReserveRequested;
            VehicleDetails.BackRequested += OnBackRequested;
            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            _addVehicleCommand = new RelayCommand(_ => OnAddVehicleRequested(), _ => CanAddVehicle);
            _backToListCommand = new RelayCommand(_ => ShowList());

            UpdateLocalizedText();
        }

        public void SetNavigationService(INavigationService navigationService)
        {
            _navigationService = navigationService;
            VehicleList.AttachNavigationService(navigationService, VehicleDetails);
        }

        public VehicleListViewModel VehicleList { get; }

        public VehicleDetailsViewModel VehicleDetails { get; }

        public bool CanAddVehicle => _userSession.CurrentUser != null;

        public ICommand AddVehicleCommand => _addVehicleCommand;

        public ICommand BackToListCommand => _backToListCommand;

        public bool ShowingDetails
        {
            get => _showingDetails;
            private set
            {
                if (SetProperty(ref _showingDetails, value))
                {
                    OnPropertyChanged(nameof(ShowingList));
                }
            }
        }

        public bool ShowingList => !ShowingDetails;

        public event EventHandler? AddVehicleRequested;

        public event EventHandler<VehicleCardViewModel?>? ReserveRequested;

        public async Task InitializeAsync()
        {
            _logger.LogInformation("VehiclesViewModel.InitializeAsync called. IsInitialized: {IsInitialized}", _isInitialized);

            if (_isInitialized)
            {
                _logger.LogInformation("Already initialized, skipping");
                return;
            }

            _isInitialized = true;
            _logger.LogInformation("Calling VehicleList.LoadVehiclesAsync");
            await VehicleList.LoadVehiclesAsync();
            _logger.LogInformation("VehicleList.LoadVehiclesAsync completed. Vehicle count: {Count}", VehicleList.Vehicles.Count);

            _logger.LogInformation("Setting initial vehicle in VehicleDetails");
            await VehicleDetails.SetVehicleAsync(VehicleList.SelectedVehicle);
            _logger.LogInformation("VehiclesViewModel initialization complete");
        }

        public async Task ReloadVehiclesAsync()
        {
            var previousId = VehicleList.SelectedVehicle?.Id;
            await VehicleList.LoadVehiclesAsync(previousId);
            await VehicleDetails.SetVehicleAsync(VehicleList.SelectedVehicle);
        }

        public override void Dispose()
        {
            base.Dispose();
            VehicleList.SelectedVehicleChanged -= OnSelectedVehicleChanged;
            VehicleDetails.ReserveRequested -= OnReserveRequested;
            VehicleDetails.BackRequested -= OnBackRequested;
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
            VehicleList.Dispose();
            VehicleDetails.Dispose();
        }

        private void OnAddVehicleRequested()
        {
            AddVehicleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectedVehicleChanged(object? sender, VehicleCardViewModel? e)
        {
            System.Diagnostics.Debug.WriteLine("=== VehiclesViewModel.OnSelectedVehicleChanged called ===");
            System.Diagnostics.Debug.WriteLine($"Selected vehicle: {e?.DisplayName ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine("Calling VehicleDetails.SetVehicleAsync...");
            _ = VehicleDetails.SetVehicleAsync(e);
        }

        private void OnBackRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== OnBackRequested called ===");
            if (_navigationService != null)
            {
                _navigationService.Navigate(this, disposePrevious: false);
            }
        }

        private void ShowList()
        {
            System.Diagnostics.Debug.WriteLine("=== ShowList called ===");
            ShowingDetails = false;
        }

        private void OnReserveRequested(object? sender, VehicleCardViewModel? e)
        {
            ReserveRequested?.Invoke(this, e);
        }

        private void OnCurrentUserChanged(object? sender, Models.User? e)
        {
            OnPropertyChanged(nameof(CanAddVehicle));
            _addVehicleCommand.RaiseCanExecuteChanged();
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("Vehicles_Title");
            Description = _localizationService.GetString("Vehicles_Description");
        }
    }
}
