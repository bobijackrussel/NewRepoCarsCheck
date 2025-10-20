using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class AdminVehicleManagementViewModel : SectionViewModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<AdminVehicleManagementViewModel> _logger;
        private readonly RelayCommand _refreshCommand;

        private bool _isLoading;
        private string? _errorMessage;

        public AdminVehicleManagementViewModel(
            IVehicleService vehicleService,
            ILocalizationService localizationService,
            ILogger<AdminVehicleManagementViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            _vehicleService = vehicleService;
            _localizationService = localizationService;
            _logger = logger;

            Vehicles = new ObservableCollection<VehicleRow>();

            _refreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);

            UpdateLocalizedText();
            _localizationService.LanguageChanged += OnLanguageChanged;

            _ = LoadAsync();
        }

        public ObservableCollection<VehicleRow> Vehicles { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    _refreshCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RefreshCommand => _refreshCommand;

        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var vehicles = await _vehicleService.GetAllAsync().ConfigureAwait(false);
                var rows = vehicles
                    .OrderByDescending(v => v.UpdatedAt)
                    .Select(v => new VehicleRow(v))
                    .ToList();

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() =>
                    {
                        Vehicles.Clear();
                        foreach (var row in rows)
                        {
                            Vehicles.Add(row);
                        }
                    });
                }
                else
                {
                    Vehicles.Clear();
                    foreach (var row in rows)
                    {
                        Vehicles.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load vehicles for admin view");
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("AdminVehicles_Title");
            Description = _localizationService.GetString("AdminVehicles_Description");
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
            foreach (var row in Vehicles)
            {
                row.RefreshText();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public class VehicleRow : BaseViewModel
        {
            private string _statusText;

            public VehicleRow(Vehicle vehicle)
            {
                Id = vehicle.Id;
                Name = $"{vehicle.ModelYear} {vehicle.Make} {vehicle.Model}".Trim();
                Category = vehicle.Category.ToString();
                Transmission = vehicle.Transmission.ToString();
                Fuel = vehicle.Fuel.ToString();
                Seats = vehicle.Seats;
                Status = vehicle.Status;
                DailyRate = vehicle.DailyRate;
                UpdatedAt = vehicle.UpdatedAt;
                _statusText = GetStatusText(vehicle.Status);
            }

            public long Id { get; }
            public string Name { get; }
            public string Category { get; }
            public string Transmission { get; }
            public string Fuel { get; }
            public byte Seats { get; }
            public VehicleStatus Status { get; }
            public decimal DailyRate { get; }
            public DateTime UpdatedAt { get; }

            public string LastUpdatedDisplay => UpdatedAt.ToString("g", CultureInfo.CurrentCulture);
            public string DailyRateDisplay => DailyRate.ToString("C", CultureInfo.CurrentCulture);

            public string StatusDisplay
            {
                get => _statusText;
                private set => SetProperty(ref _statusText, value);
            }

            public void RefreshText()
            {
                StatusDisplay = GetStatusText(Status);
                OnPropertyChanged(nameof(LastUpdatedDisplay));
                OnPropertyChanged(nameof(DailyRateDisplay));
            }

            private static string GetStatusText(VehicleStatus status)
            {
                return status switch
                {
                    VehicleStatus.Active => "Active",
                    VehicleStatus.Maintenance => "Maintenance",
                    VehicleStatus.Retired => "Retired",
                    _ => status.ToString()
                };
            }
        }
    }
}
