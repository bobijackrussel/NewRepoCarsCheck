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
        private readonly RelayCommand _addVehicleCommand;
        private readonly RelayCommand _editVehicleCommand;
        private readonly RelayCommand _toggleAvailabilityCommand;
        private readonly RelayCommand _deleteVehicleCommand;

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
            _addVehicleCommand = new RelayCommand(_ => OnAddVehicleRequested(), _ => !IsLoading);
            _editVehicleCommand = new RelayCommand(p => OnEditVehicleRequested(p as VehicleRow), p => !IsLoading && p is VehicleRow);
            _toggleAvailabilityCommand = new RelayCommand(async p => await ToggleAvailabilityAsync(p as VehicleRow), p => !IsLoading && p is VehicleRow);
            _deleteVehicleCommand = new RelayCommand(p => OnDeleteVehicleRequested(p as VehicleRow), p => !IsLoading && p is VehicleRow);

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
                    _addVehicleCommand.RaiseCanExecuteChanged();
                    _editVehicleCommand.RaiseCanExecuteChanged();
                    _toggleAvailabilityCommand.RaiseCanExecuteChanged();
                    _deleteVehicleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RefreshCommand => _refreshCommand;

        public ICommand AddVehicleCommand => _addVehicleCommand;

        public ICommand EditVehicleCommand => _editVehicleCommand;

        public ICommand ToggleAvailabilityCommand => _toggleAvailabilityCommand;

        public ICommand DeleteVehicleCommand => _deleteVehicleCommand;

        public event EventHandler? AddVehicleRequested;

        public event EventHandler<VehicleRow>? EditVehicleRequested;

        public event EventHandler<VehicleRow>? DeleteVehicleRequested;

        public Task ReloadAsync()
        {
            return LoadAsync();
        }

        private void OnAddVehicleRequested()
        {
            AddVehicleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditVehicleRequested(VehicleRow? row)
        {
            if (row == null)
            {
                return;
            }

            EditVehicleRequested?.Invoke(this, row);
        }

        private void OnDeleteVehicleRequested(VehicleRow? row)
        {
            if (row == null)
            {
                return;
            }

            DeleteVehicleRequested?.Invoke(this, row);
        }

        private async Task ToggleAvailabilityAsync(VehicleRow? row)
        {
            if (row == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var vehicle = row.ToVehicle();
                vehicle.Status = row.Status == VehicleStatus.Active
                    ? VehicleStatus.Maintenance
                    : VehicleStatus.Active;
                vehicle.UpdatedAt = DateTime.UtcNow;

                var updated = await _vehicleService.UpdateAsync(vehicle).ConfigureAwait(false);
                if (!updated)
                {
                    ErrorMessage = "Vehicle could not be updated. Please try again.";
                    return;
                }

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() => row.ApplyUpdate(vehicle));
                }
                else
                {
                    row.ApplyUpdate(vehicle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update vehicle {VehicleId}", row.Id);
                ErrorMessage = "Failed to update vehicle status.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeleteVehicleAsync(VehicleRow row)
        {
            if (row == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var success = await _vehicleService.DeleteAsync(row.Id).ConfigureAwait(false);
                if (!success)
                {
                    ErrorMessage = "Vehicle could not be deleted. Please try again.";
                    return;
                }

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() => Vehicles.Remove(row));
                }
                else
                {
                    Vehicles.Remove(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vehicle {VehicleId}", row.Id);
                ErrorMessage = "Failed to delete vehicle.";
            }
            finally
            {
                IsLoading = false;
            }
        }

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
            private readonly Vehicle _vehicle;
            private string _name;
            private string _category;
            private string _transmission;
            private string _fuel;
            private byte _seats;
            private byte _doors;
            private VehicleStatus _status;
            private decimal _dailyRate;
            private DateTime _updatedAt;
            private string _statusText;

            public VehicleRow(Vehicle vehicle)
            {
                _vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));

                Id = vehicle.Id;
                _name = $"{vehicle.ModelYear} {vehicle.Make} {vehicle.Model}".Trim();
                _category = vehicle.Category.ToString();
                _transmission = vehicle.Transmission.ToString();
                _fuel = vehicle.Fuel.ToString();
                _seats = vehicle.Seats;
                _doors = vehicle.Doors;
                _status = vehicle.Status;
                _dailyRate = vehicle.DailyRate;
                _updatedAt = vehicle.UpdatedAt;
                _statusText = GetStatusText(vehicle.Status);
            }

            public Vehicle Entity => _vehicle;

            public long Id { get; }

            public string Name
            {
                get => _name;
                private set => SetProperty(ref _name, value);
            }

            public string Category
            {
                get => _category;
                private set
                {
                    if (SetProperty(ref _category, value))
                    {
                        OnPropertyChanged(nameof(TypeDisplay));
                    }
                }
            }

            public string Transmission
            {
                get => _transmission;
                private set
                {
                    if (SetProperty(ref _transmission, value))
                    {
                        OnPropertyChanged(nameof(TypeDisplay));
                    }
                }
            }

            public string Fuel
            {
                get => _fuel;
                private set => SetProperty(ref _fuel, value);
            }

            public byte Seats
            {
                get => _seats;
                private set
                {
                    if (SetProperty(ref _seats, value))
                    {
                        _vehicle.Seats = value;
                    }
                }
            }

            public byte Doors
            {
                get => _doors;
                private set
                {
                    if (SetProperty(ref _doors, value))
                    {
                        _vehicle.Doors = value;
                    }
                }
            }

            public VehicleStatus Status
            {
                get => _status;
                private set
                {
                    if (SetProperty(ref _status, value))
                    {
                        _vehicle.Status = value;
                        StatusDisplay = GetStatusText(value);
                        OnPropertyChanged(nameof(IsActive));
                        OnPropertyChanged(nameof(AvailabilityActionText));
                    }
                }
            }

            public decimal DailyRate
            {
                get => _dailyRate;
                private set
                {
                    if (SetProperty(ref _dailyRate, value))
                    {
                        _vehicle.DailyRate = value;
                        OnPropertyChanged(nameof(DailyRateDisplay));
                        OnPropertyChanged(nameof(RatePerDayDisplay));
                    }
                }
            }

            public DateTime UpdatedAt
            {
                get => _updatedAt;
                private set
                {
                    if (SetProperty(ref _updatedAt, value))
                    {
                        _vehicle.UpdatedAt = value;
                        OnPropertyChanged(nameof(LastUpdatedDisplay));
                    }
                }
            }

            public string Description => _vehicle.Description ?? string.Empty;

            public bool IsActive => Status == VehicleStatus.Active;

            public string AvailabilityActionText => Status == VehicleStatus.Active
                ? "Mark unavailable"
                : "Mark available";

            public string TypeDisplay => string.Format(CultureInfo.CurrentCulture, "{0} • {1}", Category, Transmission);

            public string LastUpdatedDisplay => UpdatedAt.ToString("g", CultureInfo.CurrentCulture);

            public string DailyRateDisplay => DailyRate.ToString("C", CultureInfo.CurrentCulture);

            public string RatePerDayDisplay => string.Format(CultureInfo.CurrentCulture, "{0}/day", DailyRateDisplay);

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
                OnPropertyChanged(nameof(RatePerDayDisplay));
                OnPropertyChanged(nameof(TypeDisplay));
            }

            public Vehicle ToVehicle()
            {
                return new Vehicle
                {
                    Id = _vehicle.Id,
                    BranchId = _vehicle.BranchId,
                    Vin = _vehicle.Vin,
                    PlateNumber = _vehicle.PlateNumber,
                    Make = _vehicle.Make,
                    Model = _vehicle.Model,
                    ModelYear = _vehicle.ModelYear,
                    Category = _vehicle.Category,
                    Transmission = _vehicle.Transmission,
                    Fuel = _vehicle.Fuel,
                    Seats = _vehicle.Seats,
                    Doors = _vehicle.Doors,
                    Color = _vehicle.Color,
                    Description = _vehicle.Description,
                    DailyRate = DailyRate,
                    Status = Status,
                    CreatedAt = _vehicle.CreatedAt,
                    UpdatedAt = UpdatedAt
                };
            }

            public void ApplyUpdate(Vehicle updated)
            {
                if (updated == null)
                {
                    return;
                }

                _vehicle.BranchId = updated.BranchId;
                _vehicle.Vin = updated.Vin;
                _vehicle.PlateNumber = updated.PlateNumber;
                _vehicle.Make = updated.Make;
                _vehicle.Model = updated.Model;
                _vehicle.ModelYear = updated.ModelYear;
                _vehicle.Category = updated.Category;
                _vehicle.Transmission = updated.Transmission;
                _vehicle.Fuel = updated.Fuel;
                _vehicle.Seats = updated.Seats;
                _vehicle.Doors = updated.Doors;
                _vehicle.Color = updated.Color;
                _vehicle.Description = updated.Description;
                _vehicle.DailyRate = updated.DailyRate;
                _vehicle.Status = updated.Status;
                _vehicle.UpdatedAt = updated.UpdatedAt;

                Name = $"{updated.ModelYear} {updated.Make} {updated.Model}".Trim();
                Category = updated.Category.ToString();
                Transmission = updated.Transmission.ToString();
                Fuel = updated.Fuel.ToString();
                Seats = updated.Seats;
                Doors = updated.Doors;
                DailyRate = updated.DailyRate;
                Status = updated.Status;
                UpdatedAt = updated.UpdatedAt;
                OnPropertyChanged(nameof(Description));
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
