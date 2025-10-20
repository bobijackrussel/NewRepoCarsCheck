using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class ReportViolationViewModel : SectionViewModel
    {
        public const int MaxDescriptionLength = 1500;

        private readonly IViolationService _violationService;
        private readonly IReservationService _reservationService;
        private readonly IVehicleService _vehicleService;
        private readonly IUserSession _userSession;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<ReportViolationViewModel> _logger;

        private readonly ObservableCollection<ReservationOption> _reservations = new();
        private readonly ObservableCollection<ViolationHistoryItemViewModel> _violationHistory = new();

        private readonly RelayCommand _submitCommand;
        private readonly RelayCommand _refreshCommand;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isSubmitting;
        private ReservationOption? _selectedReservation;
        private ViolationType _selectedType;
        private ViolationSeverity _selectedSeverity;
        private string? _description;
        private string? _errorMessage;
        private string? _successMessage;

        public ReportViolationViewModel(
            IViolationService violationService,
            IReservationService reservationService,
            IVehicleService vehicleService,
            IUserSession userSession,
            ILocalizationService localizationService,
            ILogger<ReportViolationViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            _violationService = violationService;
            _reservationService = reservationService;
            _vehicleService = vehicleService;
            _userSession = userSession;
            _localizationService = localizationService;
            _logger = logger;

            TypeOptions = Enum.GetValues<ViolationType>();
            SeverityOptions = Enum.GetValues<ViolationSeverity>();
            _selectedType = ViolationType.LateReturn;
            _selectedSeverity = ViolationSeverity.Low;

            _submitCommand = new RelayCommand(async _ => await SubmitAsync(), _ => CanSubmit);
            _refreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsLoading);

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedText();
        }

        public ObservableCollection<ReservationOption> Reservations => _reservations;

        public ObservableCollection<ViolationHistoryItemViewModel> ViolationHistory => _violationHistory;

        public IEnumerable<ViolationType> TypeOptions { get; }

        public IEnumerable<ViolationSeverity> SeverityOptions { get; }

        public ReservationOption? SelectedReservation
        {
            get => _selectedReservation;
            set
            {
                if (SetProperty(ref _selectedReservation, value))
                {
                    _submitCommand.RaiseCanExecuteChanged();
                    SuccessMessage = null;
                }
            }
        }

        public ViolationType SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    SuccessMessage = null;
                }
            }
        }

        public ViolationSeverity SelectedSeverity
        {
            get => _selectedSeverity;
            set
            {
                if (SetProperty(ref _selectedSeverity, value))
                {
                    SuccessMessage = null;
                }
            }
        }

        public string? Description
        {
            get => _description;
            set
            {
                var newValue = value ?? string.Empty;
                if (newValue.Length > MaxDescriptionLength)
                {
                    newValue = newValue[..MaxDescriptionLength];
                }

                if (SetProperty(ref _description, newValue))
                {
                    OnPropertyChanged(nameof(DescriptionLength));
                    OnPropertyChanged(nameof(DescriptionLengthDisplay));
                    SuccessMessage = null;
                }
            }
        }

        public int DescriptionLength => string.IsNullOrEmpty(Description) ? 0 : Description.Length;

        public string DescriptionLengthDisplay => $"{DescriptionLength}/{MaxDescriptionLength} characters";

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    _refreshCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsHistoryEmpty));
                }
            }
        }

        public bool IsSubmitting
        {
            get => _isSubmitting;
            private set
            {
                if (SetProperty(ref _isSubmitting, value))
                {
                    _submitCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        SuccessMessage = null;
                    }
                }
            }
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            private set
            {
                if (SetProperty(ref _successMessage, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        ErrorMessage = null;
                    }
                }
            }
        }

        public bool HasReservations => Reservations.Count > 0;

        public bool IsHistoryEmpty => !IsLoading && ViolationHistory.Count == 0;

        public ICommand SubmitCommand => _submitCommand;

        public ICommand RefreshCommand => _refreshCommand;

        private bool CanSubmit => !IsSubmitting && _userSession.CurrentUser != null && SelectedReservation != null;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                await RefreshAsync();
                return;
            }

            _isInitialized = true;
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (IsLoading)
            {
                return;
            }

            var user = _userSession.CurrentUser;
            if (user == null)
            {
                ErrorMessage = "Sign in to report a violation.";
                SuccessMessage = null;
                _reservations.Clear();
                _violationHistory.Clear();
                OnPropertyChanged(nameof(HasReservations));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                return;
            }

            try
            {
                ErrorMessage = null;
                if (!IsSubmitting)
                {
                    SuccessMessage = null;
                }

                IsLoading = true;

                var reservationsTask = _reservationService.GetUserReservationsAsync(user.Id);
                var violationsTask = _violationService.GetByUserAsync(user.Id);
                var vehiclesTask = _vehicleService.GetAllAsync();

                await Task.WhenAll(reservationsTask, violationsTask, vehiclesTask);

                var reservations = reservationsTask.Result
                    .OrderByDescending(r => r.StartDate)
                    .ToList();

                var vehicleLookup = vehiclesTask.Result.ToDictionary(v => v.Id, v => v);

                UpdateReservations(reservations, vehicleLookup);
                UpdateHistory(violationsTask.Result, reservations, vehicleLookup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load violation data for user {UserId}", user.Id);
                ErrorMessage = "We couldn't load your violation reports. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SubmitAsync()
        {
            if (IsSubmitting)
            {
                return;
            }

            var user = _userSession.CurrentUser;
            if (user == null)
            {
                ErrorMessage = "Sign in to report a violation.";
                return;
            }

            if (SelectedReservation == null)
            {
                ErrorMessage = "Select the reservation that this violation relates to.";
                return;
            }

            try
            {
                ErrorMessage = null;
                SuccessMessage = null;
                IsSubmitting = true;

                var report = new ViolationReport
                {
                    UserId = user.Id,
                    ReservationId = SelectedReservation.Id,
                    Type = SelectedType,
                    Severity = SelectedSeverity,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    Status = ViolationStatus.Open,
                    ReportedAt = DateTime.UtcNow
                };

                var submitted = await _violationService.ReportAsync(report);
                if (submitted)
                {
                    SuccessMessage = "Thank you. Our team will review your report.";
                    await RefreshAsync();
                    ResetForm();
                }
                else
                {
                    ErrorMessage = "We couldn't submit your report. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit violation report for user {UserId}", user.Id);
                ErrorMessage = "Unexpected error while submitting your report.";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private void UpdateReservations(IEnumerable<Reservation> reservations, IReadOnlyDictionary<long, Vehicle> vehicles)
        {
            var previousSelection = SelectedReservation?.Id;
            _reservations.Clear();

            foreach (var reservation in reservations)
            {
                if (reservation.Status == ReservationStatus.Cancelled)
                {
                    continue;
                }

                var vehicle = vehicles.TryGetValue(reservation.VehicleId, out var value) ? value : null;
                var option = new ReservationOption(reservation.Id, BuildReservationDisplay(reservation, vehicle));
                _reservations.Add(option);
            }

            SelectedReservation = _reservations.FirstOrDefault(option => option.Id == previousSelection) ?? _reservations.FirstOrDefault();

            OnPropertyChanged(nameof(HasReservations));
        }

        private void UpdateHistory(IEnumerable<ViolationReport> reports, IEnumerable<Reservation> reservations, IReadOnlyDictionary<long, Vehicle> vehicles)
        {
            _violationHistory.Clear();

            var reservationLookup = reservations.ToDictionary(r => r.Id, r => r);

            foreach (var report in reports.OrderByDescending(r => r.ReportedAt))
            {
                var reservationDisplay = report.ReservationId > 0
                    ? $"Reservation #{report.ReservationId}"
                    : "Reservation info unavailable";
                if (reservationLookup.TryGetValue(report.ReservationId, out var reservation))
                {
                    var vehicle = vehicles.TryGetValue(reservation.VehicleId, out var vehicleValue) ? vehicleValue : null;
                    reservationDisplay = BuildReservationDisplay(reservation, vehicle);
                }

                _violationHistory.Add(new ViolationHistoryItemViewModel(report, reservationDisplay));
            }

            OnPropertyChanged(nameof(IsHistoryEmpty));
        }

        private void ResetForm()
        {
            SelectedType = ViolationType.LateReturn;
            SelectedSeverity = ViolationSeverity.Low;
            Description = string.Empty;
        }

        private void OnCurrentUserChanged(object? sender, User? user)
        {
            _submitCommand.RaiseCanExecuteChanged();

            if (user == null)
            {
                ResetForm();
                _reservations.Clear();
                _violationHistory.Clear();
                OnPropertyChanged(nameof(HasReservations));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                ErrorMessage = "Sign in to report a violation.";
                SuccessMessage = null;
            }
            else if (_isInitialized)
            {
                _ = RefreshAsync();
            }
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("Violations_Title");
            Description = _localizationService.GetString("Violations_Description");
        }

        private static string BuildReservationDisplay(Reservation reservation, Vehicle? vehicle)
        {
            var start = reservation.StartDate.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
            var end = reservation.EndDate.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
            var vehicleName = vehicle == null
                ? $"Vehicle #{reservation.VehicleId}"
                : FormatVehicleName(vehicle);

            return $"{vehicleName} | {start} - {end}";
        }

        private static string FormatVehicleName(Vehicle vehicle)
        {
            var year = vehicle.ModelYear > 0 ? vehicle.ModelYear.ToString(CultureInfo.CurrentCulture) + " " : string.Empty;
            return $"{year}{vehicle.Make} {vehicle.Model}".Trim();
        }

        public override void Dispose()
        {
            base.Dispose();
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }
    }

    public class ViolationHistoryItemViewModel
    {
        public ViolationHistoryItemViewModel(ViolationReport report, string reservationDisplay)
        {
            Id = report.Id;
            Type = report.Type;
            Severity = report.Severity;
            Status = report.Status;
            Description = report.Description;
            ReportedAt = report.ReportedAt;
            ReservationDisplay = reservationDisplay;
        }

        public long Id { get; }

        public ViolationType Type { get; }

        public ViolationSeverity Severity { get; }

        public ViolationStatus Status { get; }

        public string? Description { get; }

        public DateTime ReportedAt { get; }

        public string ReservationDisplay { get; }

        public string ReportedAtDisplay => ReportedAt.ToLocalTime().ToString("MMM d, yyyy · h:mm tt", CultureInfo.CurrentCulture);
    }
}
