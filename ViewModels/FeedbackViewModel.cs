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
    public class FeedbackViewModel : SectionViewModel
    {
        public const int MaxCommentLength = 1000;

        private readonly IFeedbackService _feedbackService;
        private readonly IReservationService _reservationService;
        private readonly IVehicleService _vehicleService;
        private readonly IUserSession _userSession;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<FeedbackViewModel> _logger;

        private readonly RelayCommand _submitFeedbackCommand;
        private readonly RelayCommand _refreshCommand;
        private readonly RelayCommand _setRatingCommand;

        private readonly ObservableCollection<ReservationOption> _reservations;
        private readonly ObservableCollection<FeedbackHistoryItemViewModel> _feedbackHistory;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isSubmitting;
        private byte _rating;
        private string? _comment;
        private ReservationOption? _selectedReservation;
        private string? _errorMessage;
        private string? _successMessage;

        public FeedbackViewModel(
            IFeedbackService feedbackService,
            IReservationService reservationService,
            IVehicleService vehicleService,
            IUserSession userSession,
            ILocalizationService localizationService,
            ILogger<FeedbackViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            _feedbackService = feedbackService;
            _reservationService = reservationService;
            _vehicleService = vehicleService;
            _userSession = userSession;
            _localizationService = localizationService;
            _logger = logger;

            _reservations = new ObservableCollection<ReservationOption>();
            _feedbackHistory = new ObservableCollection<FeedbackHistoryItemViewModel>();

            RatingOptions = Enumerable.Range(1, 5).ToArray();

            _submitFeedbackCommand = new RelayCommand(async _ => await SubmitFeedbackAsync(), _ => CanSubmitFeedback);
            _refreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsLoading);
            _setRatingCommand = new RelayCommand(parameter => OnRatingSelected(parameter));

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedText();
        }

        public ObservableCollection<ReservationOption> Reservations => _reservations;

        public ObservableCollection<FeedbackHistoryItemViewModel> FeedbackHistory => _feedbackHistory;

        public IEnumerable<int> RatingOptions { get; }

        public ReservationOption? SelectedReservation
        {
            get => _selectedReservation;
            set => SetProperty(ref _selectedReservation, value);
        }

        public byte Rating
        {
            get => _rating;
            set
            {
                if (SetProperty(ref _rating, value))
                {
                    _submitFeedbackCommand.RaiseCanExecuteChanged();
                    SuccessMessage = null;
                }
            }
        }

        public string? Comment
        {
            get => _comment;
            set
            {
                var newValue = value ?? string.Empty;
                if (newValue.Length > MaxCommentLength)
                {
                    newValue = newValue[..MaxCommentLength];
                }

                if (SetProperty(ref _comment, newValue))
                {
                    OnPropertyChanged(nameof(CommentLength));
                    OnPropertyChanged(nameof(CommentLengthDisplay));
                    SuccessMessage = null;
                }
            }
        }

        public int CommentLength => string.IsNullOrEmpty(Comment) ? 0 : Comment.Length;

        public string CommentLengthDisplay => $"{CommentLength}/{MaxCommentLength} characters";

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
                    _submitFeedbackCommand.RaiseCanExecuteChanged();
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

        public bool IsHistoryEmpty => !IsLoading && FeedbackHistory.Count == 0;

        public ICommand SubmitFeedbackCommand => _submitFeedbackCommand;

        public ICommand RefreshCommand => _refreshCommand;

        public ICommand SetRatingCommand => _setRatingCommand;

        private bool CanSubmitFeedback => !IsSubmitting && Rating > 0 && _userSession.CurrentUser != null;

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
                ErrorMessage = "Sign in to leave feedback.";
                SuccessMessage = null;
                _reservations.Clear();
                _feedbackHistory.Clear();
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
                var feedbackTask = _feedbackService.GetByUserAsync(user.Id);
                var vehiclesTask = _vehicleService.GetAllAsync();

                await Task.WhenAll(reservationsTask, feedbackTask, vehiclesTask);

                var reservations = reservationsTask.Result.OrderByDescending(r => r.StartDate).ToList();
                var vehiclesLookup = vehiclesTask.Result.ToDictionary(v => v.Id, v => v);

                UpdateReservations(reservations, vehiclesLookup);
                UpdateFeedbackHistory(feedbackTask.Result, reservations, vehiclesLookup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load feedback data for user {UserId}", user.Id);
                ErrorMessage = "We couldn't load your feedback information. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SubmitFeedbackAsync()
        {
            if (IsSubmitting)
            {
                return;
            }

            var user = _userSession.CurrentUser;
            if (user == null)
            {
                ErrorMessage = "Sign in to leave feedback.";
                return;
            }

            if (Rating <= 0)
            {
                ErrorMessage = "Please select a star rating before submitting.";
                return;
            }

            try
            {
                ErrorMessage = null;
                SuccessMessage = null;
                IsSubmitting = true;

                var feedback = new Feedback
                {
                    UserId = user.Id,
                    ReservationId = SelectedReservation?.Id,
                    Rating = Rating,
                    Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim()
                };

                var submitted = await _feedbackService.SubmitAsync(feedback);
                if (submitted)
                {
                    SuccessMessage = "Thank you for sharing your experience!";
                    await RefreshAsync();
                    ResetForm();
                }
                else
                {
                    ErrorMessage = "We couldn't submit your feedback. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit feedback for user {UserId}", user.Id);
                ErrorMessage = "Unexpected error while submitting your feedback.";
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

                var vehicle = vehicles.TryGetValue(reservation.VehicleId, out var vehicleValue) ? vehicleValue : null;
                var option = new ReservationOption(reservation.Id, BuildReservationDisplay(reservation, vehicle));
                _reservations.Add(option);
            }

            SelectedReservation = _reservations.FirstOrDefault(option => option.Id == previousSelection) ?? _reservations.FirstOrDefault();

            OnPropertyChanged(nameof(HasReservations));
        }

        private void UpdateFeedbackHistory(IEnumerable<Feedback> feedbackEntries, IEnumerable<Reservation> reservations, IReadOnlyDictionary<long, Vehicle> vehicles)
        {
            _feedbackHistory.Clear();

            var reservationLookup = reservations.ToDictionary(r => r.Id, r => r);

            foreach (var feedback in feedbackEntries)
            {
                string? reservationDisplay = null;
                if (feedback.ReservationId.HasValue && reservationLookup.TryGetValue(feedback.ReservationId.Value, out var reservation))
                {
                    var vehicle = vehicles.TryGetValue(reservation.VehicleId, out var vehicleValue) ? vehicleValue : null;
                    reservationDisplay = BuildReservationDisplay(reservation, vehicle);
                }

                _feedbackHistory.Add(new FeedbackHistoryItemViewModel(feedback, reservationDisplay));
            }

            OnPropertyChanged(nameof(IsHistoryEmpty));
        }

        private void ResetForm()
        {
            Rating = 0;
            Comment = string.Empty;
            SelectedReservation = null;
        }

        private void OnRatingSelected(object? parameter)
        {
            if (parameter is int intRating)
            {
                Rating = (byte)intRating;
            }
            else if (parameter is string ratingText && int.TryParse(ratingText, out var parsed))
            {
                Rating = (byte)parsed;
            }
        }

        private void OnCurrentUserChanged(object? sender, User? user)
        {
            _submitFeedbackCommand.RaiseCanExecuteChanged();

            if (user == null)
            {
                ResetForm();
                _reservations.Clear();
                _feedbackHistory.Clear();
                OnPropertyChanged(nameof(HasReservations));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                ErrorMessage = "Sign in to leave feedback.";
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
            Title = _localizationService.GetString("Feedback_Title");
            Description = _localizationService.GetString("Feedback_Description");
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

    public class ReservationOption
    {
        public ReservationOption(long id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public long Id { get; }

        public string DisplayName { get; }
    }

    public class FeedbackHistoryItemViewModel
    {
        public FeedbackHistoryItemViewModel(Feedback feedback, string? reservationDisplay)
        {
            Id = feedback.Id;
            Rating = feedback.Rating;
            Comment = feedback.Comment;
            CreatedAt = feedback.CreatedAt;
            ReservationDisplay = reservationDisplay;
        }

        public long Id { get; }

        public byte Rating { get; }

        public string? Comment { get; }

        public DateTime CreatedAt { get; }

        public string? ReservationDisplay { get; }

        public string RatingStars => new string('*', Rating) + new string('.', Math.Max(0, 5 - Rating));

        public string CreatedDisplay => CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.CurrentCulture);

        public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
    }
}


