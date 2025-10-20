using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class CreateReservationViewModel : BaseViewModel
    {
        private readonly IReservationService _reservationService;
        private readonly IVehicleService _vehicleService;
        private readonly IUserSession _userSession;
        private readonly ILogger<CreateReservationViewModel> _logger;
        private readonly RelayCommand _saveCommand;
        private readonly RelayCommand _cancelCommand;

        private VehicleOption? _selectedVehicle;
        private DateTime _startDate;
        private DateTime _endDate;
        private decimal _totalAmount;
        private int _rentalDays;
        private string? _notes;
        private bool _isSaving;
        private string? _errorMessage;
        private bool _allowVehicleSelection;

        public CreateReservationViewModel(
            IReservationService reservationService,
            IVehicleService vehicleService,
            IUserSession userSession,
            ILogger<CreateReservationViewModel> logger)
        {
            _reservationService = reservationService;
            _vehicleService = vehicleService;
            _userSession = userSession;
            _logger = logger;

            AvailableVehicles = new ObservableCollection<VehicleOption>();

            _startDate = DateTime.Today.AddDays(1);
            _endDate = _startDate.AddDays(1);

            _saveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            _cancelCommand = new RelayCommand(_ => RequestClose(false));
        }

        public ObservableCollection<VehicleOption> AvailableVehicles { get; }

        public VehicleOption? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (SetProperty(ref _selectedVehicle, value))
                {
                    UpdateTotals();
                    _saveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(ShowVehicleRate));
                    OnPropertyChanged(nameof(VehicleRateDisplay));
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                value = value.Date;
                if (SetProperty(ref _startDate, value))
                {
                    if (EndDate <= _startDate)
                    {
                        EndDate = _startDate.AddDays(1);
                    }

                    UpdateTotals();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                value = value.Date;
                if (SetProperty(ref _endDate, value))
                {
                    if (_endDate <= StartDate)
                    {
                        _endDate = StartDate.AddDays(1);
                        OnPropertyChanged(nameof(EndDate));
                    }

                    UpdateTotals();
                }
            }
        }

        public int RentalDays
        {
            get => _rentalDays;
            private set
            {
                if (SetProperty(ref _rentalDays, value))
                {
                    OnPropertyChanged(nameof(RentalDaysDisplay));
                }
            }
        }

        public string RentalDaysDisplay => RentalDays <= 0
            ? "0 nights"
            : RentalDays == 1
                ? "1 night"
                : string.Format(CultureInfo.CurrentCulture, "{0} nights", RentalDays);

        public decimal TotalAmount
        {
            get => _totalAmount;
            private set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    OnPropertyChanged(nameof(TotalAmountDisplay));
                }
            }
        }

        public string TotalAmountDisplay => TotalAmount.ToString("C", CultureInfo.CurrentCulture);

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

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

        public bool ShowVehicleSelector => _allowVehicleSelection;

        public bool ShowVehicleRate => SelectedVehicle != null;

        public string VehicleRateDisplay => SelectedVehicle?.DailyRate.ToString("C", CultureInfo.CurrentCulture) ?? string.Empty;

        public ICommand SaveCommand => _saveCommand;

        public ICommand CancelCommand => _cancelCommand;

        public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

        public async Task InitializeAsync(VehicleCardViewModel? vehicleContext = null)
        {
            ErrorMessage = null;
            AvailableVehicles.Clear();

            if (vehicleContext != null)
            {
                _allowVehicleSelection = false;
                var option = VehicleOption.FromVehicle(vehicleContext.Entity);
                AvailableVehicles.Add(option);
                SelectedVehicle = option;
            }
            else
            {
                _allowVehicleSelection = true;
                try
                {
                    var vehicles = await _vehicleService.GetAllAsync();
                    foreach (var vehicle in vehicles.Where(v => v.Status == VehicleStatus.Active))
                    {
                        AvailableVehicles.Add(VehicleOption.FromVehicle(vehicle));
                    }

                    SelectedVehicle = AvailableVehicles.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load vehicles for reservation dialog");
                    ErrorMessage = "Unable to load vehicles right now. Please try again later.";
                }
            }

            StartDate = DateTime.Today.AddDays(1);
            EndDate = StartDate.AddDays(1);
            Notes = null;

            OnPropertyChanged(nameof(ShowVehicleSelector));
            _saveCommand.RaiseCanExecuteChanged();

            if (!AvailableVehicles.Any())
            {
                ErrorMessage = "No active vehicles are available for reservation.";
            }
        }

        private bool CanSave()
        {
            return !IsSaving
                   && SelectedVehicle != null
                   && RentalDays > 0;
        }

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            ErrorMessage = null;

            var user = _userSession.CurrentUser;
            if (user == null)
            {
                ErrorMessage = "You must be signed in to create a reservation.";
                return;
            }

            if (SelectedVehicle == null)
            {
                ErrorMessage = "Please select a vehicle.";
                return;
            }

            if (RentalDays <= 0)
            {
                ErrorMessage = "The end date must be after the start date.";
                return;
            }

            var reservation = new Reservation
            {
                UserId = user.Id,
                VehicleId = SelectedVehicle.VehicleId,
                StartDate = StartDate,
                EndDate = EndDate,
                Status = ReservationStatus.Pending,
                TotalAmount = TotalAmount,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim()
            };

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(reservation);
            if (!Validator.TryValidateObject(reservation, context, validationResults, validateAllProperties: true))
            {
                ErrorMessage = validationResults.FirstOrDefault()?.ErrorMessage ?? "Some fields are invalid.";
                return;
            }

            try
            {
                IsSaving = true;

                var isAvailable = await _reservationService.IsVehicleAvailableAsync(
                    reservation.VehicleId,
                    reservation.StartDate,
                    reservation.EndDate);

                if (!isAvailable)
                {
                    ErrorMessage = "This vehicle is no longer available for the selected dates.";
                    return;
                }

                var success = await _reservationService.CreateAsync(reservation);
                if (!success)
                {
                    ErrorMessage = "The reservation could not be created. Please try again.";
                    return;
                }

                RequestClose(true);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Reservation validation failed for vehicle {VehicleId}", reservation.VehicleId);
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create reservation");
                ErrorMessage = "Unexpected error while creating the reservation.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void UpdateTotals()
        {
            if (SelectedVehicle == null)
            {
                RentalDays = 0;
                TotalAmount = 0m;
                return;
            }

            var duration = (EndDate - StartDate).TotalDays;
            RentalDays = duration <= 0 ? 0 : (int)Math.Ceiling(duration);
            TotalAmount = RentalDays <= 0 ? 0m : RentalDays * SelectedVehicle.DailyRate;
        }

        private void RequestClose(bool result)
        {
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(result));
        }

        public class VehicleOption
        {
            private VehicleOption(long vehicleId, string name, decimal dailyRate)
            {
                VehicleId = vehicleId;
                DisplayName = name;
                DailyRate = dailyRate;
            }

            public long VehicleId { get; }

            public string DisplayName { get; }

            public decimal DailyRate { get; }

            public static VehicleOption FromVehicle(Vehicle vehicle)
            {
                if (vehicle == null)
                {
                    throw new ArgumentNullException(nameof(vehicle));
                }

                var name = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2}",
                    vehicle.ModelYear,
                    vehicle.Make,
                    vehicle.Model).Trim();

                return new VehicleOption(vehicle.Id, name, vehicle.DailyRate);
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}
