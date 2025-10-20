using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CarRentalManagment.ViewModels
{
    public class AddVehicleViewModel : BaseViewModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<AddVehicleViewModel> _logger;
        private readonly RelayCommand _saveCommand;
        private readonly RelayCommand _cancelCommand;

        private bool _isSaving;
        private string? _errorMessage;

        public AddVehicleViewModel(IVehicleService vehicleService, ILogger<AddVehicleViewModel> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;

            Vehicle = new Vehicle
            {
                ModelYear = (short)DateTime.Now.Year,
                Seats = 4,
                Doors = 4,
                DailyRate = 50,
                Category = VehicleCategory.Economy,
                Transmission = TransmissionType.Automatic,
                Fuel = FuelType.Gasoline,
                Status = VehicleStatus.Active
            };

            Categories = new ObservableCollection<VehicleCategory>(Enum.GetValues(typeof(VehicleCategory)).Cast<VehicleCategory>());
            Transmissions = new ObservableCollection<TransmissionType>(Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>());
            FuelTypes = new ObservableCollection<FuelType>(Enum.GetValues(typeof(FuelType)).Cast<FuelType>());
            Statuses = new ObservableCollection<VehicleStatus>(Enum.GetValues(typeof(VehicleStatus)).Cast<VehicleStatus>());

            _saveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            _cancelCommand = new RelayCommand(_ => RequestClose(false));
        }

        public Vehicle Vehicle { get; }

        public ObservableCollection<VehicleCategory> Categories { get; }

        public ObservableCollection<TransmissionType> Transmissions { get; }

        public ObservableCollection<FuelType> FuelTypes { get; }

        public ObservableCollection<VehicleStatus> Statuses { get; }

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    _saveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SaveCommand => _saveCommand;

        public ICommand CancelCommand => _cancelCommand;

        public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            ErrorMessage = null;

            if (Vehicle.DailyRate <= 0)
            {
                ErrorMessage = "Daily rate must be greater than zero.";
                return;
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(Vehicle);
            if (!Validator.TryValidateObject(Vehicle, validationContext, validationResults, validateAllProperties: true))
            {
                ErrorMessage = validationResults.FirstOrDefault()?.ErrorMessage ?? "Some fields are invalid.";
                return;
            }

            try
            {
                IsSaving = true;
                var created = await _vehicleService.CreateAsync(Vehicle);
                if (!created)
                {
                    ErrorMessage = "Vehicle could not be created. Please try again.";
                    return;
                }

                RequestClose(true);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "Failed to create vehicle due to database error");
                ErrorMessage = ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry
                    ? "The VIN or plate number already exists."
                    : "Database error while creating the vehicle.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating vehicle");
                ErrorMessage = "Unexpected error while creating the vehicle.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void RequestClose(bool dialogResult)
        {
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(dialogResult));
        }
    }

    public class DialogCloseRequestedEventArgs : EventArgs
    {
        public DialogCloseRequestedEventArgs(bool dialogResult)
        {
            DialogResult = dialogResult;
        }

        public bool DialogResult { get; }
    }
}
