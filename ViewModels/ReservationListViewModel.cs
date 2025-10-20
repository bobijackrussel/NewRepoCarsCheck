using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class ReservationListViewModel : BaseViewModel
    {
        private readonly IReservationService _reservationService;
        private readonly IVehicleService _vehicleService;
        private readonly IUserSession _userSession;
        private readonly ILogger<ReservationListViewModel> _logger;

        private readonly RelayCommand _refreshCommand;
        private readonly RelayCommand _cancelReservationCommand;

        private readonly ObservableCollection<ReservationItemViewModel> _allReservations;
        private readonly CollectionViewSource _reservationsViewSource;

        private ReservationFilterOption? _selectedFilter;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isInitialized;

        public ReservationListViewModel(
            IReservationService reservationService,
            IVehicleService vehicleService,
            IUserSession userSession,
            ILogger<ReservationListViewModel> logger)
        {
            _reservationService = reservationService;
            _vehicleService = vehicleService;
            _userSession = userSession;
            _logger = logger;

            _userSession.CurrentUserChanged += OnCurrentUserChanged;

            _allReservations = new ObservableCollection<ReservationItemViewModel>();
            _allReservations.CollectionChanged += OnReservationsCollectionChanged;
            _reservationsViewSource = new CollectionViewSource { Source = _allReservations };
            _reservationsViewSource.Filter += OnReservationsFilter;

            Filters = new ObservableCollection<ReservationFilterOption>(ReservationFilterOption.CreateDefaults());
            _selectedFilter = Filters.FirstOrDefault();

            _refreshCommand = new RelayCommand(async _ => await LoadReservationsAsync());
            _cancelReservationCommand = new RelayCommand(reservation => OnCancelReservation(reservation as ReservationItemViewModel));
        }

        public ObservableCollection<ReservationFilterOption> Filters { get; }

        public ICollectionView Reservations => _reservationsViewSource.View;

        public ReservationFilterOption? SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    _reservationsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsEmpty => !IsLoading && (_reservationsViewSource.View.IsEmpty || _reservationsViewSource.View.Cast<object>().Count() == 0);

        public ICommand RefreshCommand => _refreshCommand;

        public ICommand CancelReservationCommand => _cancelReservationCommand;

        public event EventHandler<ReservationItemViewModel>? CancelReservationRequested;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            await LoadReservationsAsync();
        }

        public async Task LoadReservationsAsync()
        {
            if (IsLoading)
            {
                return;
            }

            ErrorMessage = null;

            var user = _userSession.CurrentUser;
            if (user == null)
            {
                _allReservations.Clear();
                _reservationsViewSource.View.Refresh();
                OnPropertyChanged(nameof(IsEmpty));
                ErrorMessage = "Sign in to view your reservations.";
                return;
            }

            try
            {
                IsLoading = true;

                var reservationsTask = _reservationService.GetUserReservationsAsync(user.Id);
                var vehiclesTask = _vehicleService.GetAllAsync();

                await Task.WhenAll(reservationsTask, vehiclesTask);

                var reservations = reservationsTask.Result.OrderByDescending(r => r.StartDate).ToList();
                var vehiclesLookup = vehiclesTask.Result.ToDictionary(v => v.Id, v => v);

                _allReservations.Clear();

                foreach (var reservation in reservations)
                {
                    var vehicle = vehiclesLookup.TryGetValue(reservation.VehicleId, out var value) ? value : null;
                    _allReservations.Add(new ReservationItemViewModel(reservation, vehicle));
                }

                _reservationsViewSource.View.Refresh();
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load reservations");
                ErrorMessage = "Unable to load reservations right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CancelReservationAsync(ReservationItemViewModel reservation, string? reason = null)
        {
            if (reservation == null || reservation.IsCancelling)
            {
                return;
            }

            try
            {
                ErrorMessage = null;
                reservation.IsCancelling = true;
                var cancelled = await _reservationService.CancelAsync(reservation.Id, reason);
                if (cancelled)
                {
                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.CancellationReason = reason;
                    reservation.CancelledAt = DateTime.UtcNow;
                    _reservationsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                }
                else
                {
                    ErrorMessage = "The reservation could not be cancelled.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel reservation {ReservationId}", reservation.Id);
                ErrorMessage = "Unexpected error while cancelling the reservation.";
            }
            finally
            {
                reservation.IsCancelling = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _reservationsViewSource.Filter -= OnReservationsFilter;
            _allReservations.CollectionChanged -= OnReservationsCollectionChanged;
        }

        private void OnReservationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void OnReservationsFilter(object sender, FilterEventArgs e)
        {
            if (SelectedFilter == null)
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is ReservationItemViewModel reservation)
            {
                e.Accepted = SelectedFilter.Matches(reservation.Status);
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void OnCancelReservation(ReservationItemViewModel? reservation)
        {
            if (reservation == null || !reservation.CanCancel)
            {
                return;
            }

            CancelReservationRequested?.Invoke(this, reservation);
        }

        private async void OnCurrentUserChanged(object? sender, User? e)
        {
            await LoadReservationsAsync();
        }
    }

    public class ReservationItemViewModel : BaseViewModel
    {
        private ReservationStatus _status;
        private bool _isCancelling;
        private string? _cancellationReason;
        private DateTime? _cancelledAt;

        public ReservationItemViewModel(Reservation reservation, Vehicle? vehicle)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(nameof(reservation));
            }

            Entity = reservation;
            Vehicle = vehicle;
            _status = reservation.Status;
            _cancellationReason = reservation.CancellationReason;
            _cancelledAt = reservation.CancelledAt;
        }

        public Reservation Entity { get; }

        public Vehicle? Vehicle { get; }

        public long Id => Entity.Id;

        public long VehicleId => Entity.VehicleId;

        public DateTime StartDate => Entity.StartDate;

        public DateTime EndDate => Entity.EndDate;

        public ReservationStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    Entity.Status = value;
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public decimal TotalAmount => Entity.TotalAmount;

        public string? Notes => Entity.Notes;

        public DateTime CreatedAt => Entity.CreatedAt;

        public DateTime UpdatedAt => Entity.UpdatedAt;

        public DateTime? CancelledAt
        {
            get => _cancelledAt;
            set
            {
                if (SetProperty(ref _cancelledAt, value))
                {
                    Entity.CancelledAt = value;
                }
            }
        }

        public string? CancellationReason
        {
            get => _cancellationReason;
            set
            {
                if (SetProperty(ref _cancellationReason, value))
                {
                    Entity.CancellationReason = value;
                }
            }
        }

        public bool IsCancelling
        {
            get => _isCancelling;
            set => SetProperty(ref _isCancelling, value);
        }

        public string VehicleDisplayName
        {
            get
            {
                if (Vehicle != null)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1} {2}",
                        Vehicle.ModelYear,
                        Vehicle.Make,
                        Vehicle.Model).Trim();
                }

                return $"Vehicle #{VehicleId}";
            }
        }

        public string StatusDisplay => Status.ToString();

        public string DateRangeDisplay => string.Format(
            CultureInfo.CurrentCulture,
            "{0:d} - {1:d}",
            StartDate,
            EndDate);

        public string TotalAmountDisplay => TotalAmount.ToString("C", CultureInfo.CurrentCulture);

        public bool CanCancel => Status is ReservationStatus.Pending or ReservationStatus.Confirmed;
    }

    public class ReservationFilterOption
    {
        private ReservationFilterOption(string displayName, params ReservationStatus[] statuses)
        {
            DisplayName = displayName;
            Statuses = statuses ?? Array.Empty<ReservationStatus>();
        }

        public string DisplayName { get; }

        public IReadOnlyList<ReservationStatus> Statuses { get; }

        public bool Matches(ReservationStatus status)
        {
            if (Statuses.Count == 0)
            {
                return true;
            }

            return Statuses.Contains(status);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static IEnumerable<ReservationFilterOption> CreateDefaults()
        {
            yield return new ReservationFilterOption("All", Array.Empty<ReservationStatus>());
            yield return new ReservationFilterOption("Pending", ReservationStatus.Pending);
            yield return new ReservationFilterOption("Confirmed", ReservationStatus.Confirmed);
            yield return new ReservationFilterOption("Completed", ReservationStatus.Completed);
            yield return new ReservationFilterOption("Cancelled", ReservationStatus.Cancelled);
        }
    }
}
