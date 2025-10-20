using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class ReservationsViewModel : SectionViewModel
    {
        private readonly IUserSession _userSession;
        private readonly ILocalizationService _localizationService;
        private readonly RelayCommand _createReservationCommand;
        private bool _isInitialized;

        public ReservationsViewModel(
            ReservationListViewModel reservationList,
            IUserSession userSession,
            ILocalizationService localizationService)
            : base(string.Empty, string.Empty)
        {
            ReservationList = reservationList;
            _userSession = userSession;
            _localizationService = localizationService;

            _createReservationCommand = new RelayCommand(_ => OnCreateReservationRequested(), _ => CanCreateReservation);

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedText();
        }

        public ReservationListViewModel ReservationList { get; }

        public bool CanCreateReservation => _userSession.CurrentUser != null;

        public ICommand CreateReservationCommand => _createReservationCommand;

        public event EventHandler? CreateReservationRequested;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            await ReservationList.InitializeAsync();
        }

        public async Task RefreshAsync()
        {
            await ReservationList.LoadReservationsAsync();
        }

        public override void Dispose()
        {
            base.Dispose();
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
            ReservationList.Dispose();
        }

        private void OnCreateReservationRequested()
        {
            CreateReservationRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCurrentUserChanged(object? sender, User? e)
        {
            _createReservationCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanCreateReservation));
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("Reservations_Title");
            Description = _localizationService.GetString("Reservations_Description");
        }
    }
}
