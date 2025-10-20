using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class AdminReservationManagementViewModel : SectionViewModel
    {
        private readonly IReservationService _reservationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<AdminReservationManagementViewModel> _logger;
        private readonly RelayCommand _refreshCommand;

        private bool _isLoading;
        private string? _errorMessage;

        public AdminReservationManagementViewModel(
            IReservationService reservationService,
            ILocalizationService localizationService,
            ILogger<AdminReservationManagementViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            _reservationService = reservationService;
            _localizationService = localizationService;
            _logger = logger;

            Reservations = new ObservableCollection<ReservationRow>();
            _refreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);

            UpdateLocalizedText();
            _localizationService.LanguageChanged += OnLanguageChanged;

            _ = LoadAsync();
        }

        public ObservableCollection<ReservationRow> Reservations { get; }

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

                var reservations = await _reservationService.GetAllAsync().ConfigureAwait(false);
                var rows = reservations
                    .OrderByDescending(r => r.StartDate)
                    .Select(r => new ReservationRow(r))
                    .ToList();

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() =>
                    {
                        Reservations.Clear();
                        foreach (var row in rows)
                        {
                            Reservations.Add(row);
                        }
                    });
                }
                else
                {
                    Reservations.Clear();
                    foreach (var row in rows)
                    {
                        Reservations.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load reservations for admin view");
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("AdminReservations_Title");
            Description = _localizationService.GetString("AdminReservations_Description");
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
            foreach (var row in Reservations)
            {
                row.RefreshText();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public class ReservationRow : BaseViewModel
        {
            private string _statusDisplay;

            public ReservationRow(Reservation reservation)
            {
                Id = reservation.Id;
                UserId = reservation.UserId;
                VehicleId = reservation.VehicleId;
                Status = reservation.Status;
                StartDate = reservation.StartDate;
                EndDate = reservation.EndDate;
                TotalAmount = reservation.TotalAmount;
                Notes = reservation.Notes ?? string.Empty;
                CancellationReason = reservation.CancellationReason ?? string.Empty;
                _statusDisplay = GetStatusText(reservation.Status);
            }

            public long Id { get; }
            public long UserId { get; }
            public long VehicleId { get; }
            public ReservationStatus Status { get; }
            public DateTime StartDate { get; }
            public DateTime EndDate { get; }
            public decimal TotalAmount { get; }
            public string Notes { get; }
            public string CancellationReason { get; }

            public int DurationDays => Math.Max(1, (int)Math.Round((EndDate - StartDate).TotalDays));

            public string StartDateDisplay => StartDate.ToString("d", CultureInfo.CurrentCulture);
            public string EndDateDisplay => EndDate.ToString("d", CultureInfo.CurrentCulture);
            public string TotalAmountDisplay => TotalAmount.ToString("C", CultureInfo.CurrentCulture);

            public string ReservationNumberDisplay => $"Reservation #{Id:D6}";

            public string VehicleDisplay => string.Format(CultureInfo.CurrentCulture, "Vehicle #{0}", VehicleId);

            public string RentalPeriodDisplay => string.Format(
                CultureInfo.CurrentCulture,
                "{0:MMM d, yyyy} - {1:MMM d, yyyy}",
                StartDate,
                EndDate);

            public string UserDisplay => string.Format(CultureInfo.CurrentCulture, "User #{0}", UserId);

            public string DurationDisplay => DurationDays == 1
                ? "1 day"
                : string.Format(CultureInfo.CurrentCulture, "{0} days", DurationDays);

            public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

            public string NotesDisplay => HasNotes
                ? Notes
                : "No notes provided.";

            public string StatusDisplay
            {
                get => _statusDisplay;
                private set => SetProperty(ref _statusDisplay, value);
            }

            public void RefreshText()
            {
                StatusDisplay = GetStatusText(Status);
                OnPropertyChanged(nameof(StartDateDisplay));
                OnPropertyChanged(nameof(EndDateDisplay));
                OnPropertyChanged(nameof(TotalAmountDisplay));
                OnPropertyChanged(nameof(RentalPeriodDisplay));
                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(NotesDisplay));
            }

            private static string GetStatusText(ReservationStatus status)
            {
                return status switch
                {
                    ReservationStatus.Pending => "Pending",
                    ReservationStatus.Confirmed => "Confirmed",
                    ReservationStatus.Cancelled => "Cancelled",
                    ReservationStatus.Completed => "Completed",
                    _ => status.ToString()
                };
            }
        }
    }
}
