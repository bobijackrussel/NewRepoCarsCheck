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

namespace CarRentalManagment.ViewModels
{
    public class EditVehicleViewModel : BaseViewModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<EditVehicleViewModel> _logger;
        private readonly RelayCommand _saveCommand;
        private readonly RelayCommand _cancelCommand;

        private Vehicle _vehicle = new();
        private Vehicle? _originalVehicle;
        private bool _isSaving;
        private string? _errorMessage;

        public EditVehicleViewModel(IVehicleService vehicleService, ILogger<EditVehicleViewModel> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;

            Categories = new ObservableCollection<VehicleCategory>(Enum.GetValues(typeof(VehicleCategory)).Cast<VehicleCategory>());
            Transmissions = new ObservableCollection<TransmissionType>(Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>());
            FuelTypes = new ObservableCollection<FuelType>(Enum.GetValues(typeof(FuelType)).Cast<FuelType>());
            Statuses = new ObservableCollection<VehicleStatus>(Enum.GetValues(typeof(VehicleStatus)).Cast<VehicleStatus>());

            _saveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            _cancelCommand = new RelayCommand(_ => RequestClose(false));
        }

        public ObservableCollection<VehicleCategory> Categories { get; }

        public ObservableCollection<TransmissionType> Transmissions { get; }

        public ObservableCollection<FuelType> FuelTypes { get; }

        public ObservableCollection<VehicleStatus> Statuses { get; }

        public Vehicle Vehicle
        {
            get => _vehicle;
            private set => SetProperty(ref _vehicle, value);
        }

        public string DialogTitle => "Edit vehicle";

        public string DialogDescription => "Update vehicle information.";

        public string PrimaryButtonText => "Update vehicle";

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

        public Task InitializeAsync(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            _originalVehicle = vehicle;
            Vehicle = CloneVehicle(vehicle);
            ErrorMessage = null;

            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (_originalVehicle == null)
            {
                ErrorMessage = "Unable to load vehicle details.";
                return;
            }

            if (IsSaving)
            {
                return;
            }

            ErrorMessage = null;

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

                var updatedVehicle = CloneVehicle(Vehicle);
                updatedVehicle.Id = _originalVehicle.Id;
                updatedVehicle.CreatedAt = _originalVehicle.CreatedAt;
                updatedVehicle.UpdatedAt = DateTime.UtcNow;

                var success = await _vehicleService.UpdateAsync(updatedVehicle).ConfigureAwait(false);
                if (!success)
                {
                    ErrorMessage = "Vehicle could not be updated. Please try again.";
                    return;
                }

                RequestClose(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update vehicle {VehicleId}", _originalVehicle.Id);
                ErrorMessage = "Unexpected error while updating the vehicle.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static Vehicle CloneVehicle(Vehicle source)
        {
            return new Vehicle
            {
                Id = source.Id,
                BranchId = source.BranchId,
                Vin = source.Vin,
                PlateNumber = source.PlateNumber,
                Make = source.Make,
                Model = source.Model,
                ModelYear = source.ModelYear,
                Category = source.Category,
                Transmission = source.Transmission,
                Fuel = source.Fuel,
                Seats = source.Seats,
                Doors = source.Doors,
                Color = source.Color,
                Description = source.Description,
                DailyRate = source.DailyRate,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        private void RequestClose(bool dialogResult)
        {
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(dialogResult));
        }
    }
}
