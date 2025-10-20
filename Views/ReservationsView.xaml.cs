using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.Utilities;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class ReservationsView : UserControl
    {
        public ReservationsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReservationsViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private async Task InitializeAsync(ReservationsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ReservationsViewModel oldViewModel)
            {
                oldViewModel.CreateReservationRequested -= OnCreateReservationRequested;
                oldViewModel.ReservationList.CancelReservationRequested -= OnCancelReservationRequested;
            }

            if (e.NewValue is ReservationsViewModel newViewModel)
            {
                newViewModel.CreateReservationRequested += OnCreateReservationRequested;
                newViewModel.ReservationList.CancelReservationRequested += OnCancelReservationRequested;
                await InitializeAsync(newViewModel);
            }
        }

        private async void OnCreateReservationRequested(object? sender, EventArgs e)
        {
            if (DataContext is not ReservationsViewModel viewModel)
            {
                return;
            }

            var dialog = CreateReservationDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is CreateReservationViewModel reservationViewModel)
            {
                await reservationViewModel.InitializeAsync();

                void OnDialogClose(object? _, DialogCloseRequestedEventArgs args)
                {
                    dialog.DialogResult = args.DialogResult;
                    dialog.Close();
                }

                reservationViewModel.CloseRequested += OnDialogClose;
                dialog.Closed += (_, _) => reservationViewModel.CloseRequested -= OnDialogClose;
            }

            var result = dialog.ShowDialog();
            if (result == true)
            {
                await viewModel.RefreshAsync();
            }
        }

        private async void OnCancelReservationRequested(object? sender, ReservationItemViewModel e)
        {
            if (DataContext is not ReservationsViewModel viewModel)
            {
                return;
            }

            var message = $"Are you sure you want to cancel the reservation for {e.VehicleDisplayName}?";
            var confirmation = MessageBox.Show(message, "Cancel Reservation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmation == MessageBoxResult.Yes)
            {
                await viewModel.ReservationList.CancelReservationAsync(e);
            }
        }

        private CreateReservationDialog CreateReservationDialog()
        {
            return new CreateReservationDialog
            {
                DataContext = AppServices.GetRequiredService<CreateReservationViewModel>()
            };
        }
    }
}
