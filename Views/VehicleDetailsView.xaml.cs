using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.Utilities;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class VehicleDetailsView : UserControl
    {
        private VehicleDetailsViewModel? _viewModel;

        public VehicleDetailsView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ReserveRequested -= OnReserveRequested;
            }

            _viewModel = DataContext as VehicleDetailsViewModel;
            System.Diagnostics.Debug.WriteLine($"VehicleDetailsView DataContext changed to: {_viewModel?.GetType().Name ?? "null"}");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== VehicleDetailsView LOADED ===");

            if (_viewModel != null)
            {
                _viewModel.ReserveRequested += OnReserveRequested;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WARNING: _viewModel is null in OnLoaded");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== VehicleDetailsView UNLOADED ===");

            if (_viewModel != null)
            {
                _viewModel.ReserveRequested -= OnReserveRequested;
            }
        }

        private async void OnReserveRequested(object? sender, VehicleCardViewModel? e)
        {
            System.Diagnostics.Debug.WriteLine($"=== OnReserveRequested called for vehicle: {e?.DisplayName ?? "null"} ===");

            if (e == null)
            {
                System.Diagnostics.Debug.WriteLine("Vehicle is null, cannot create reservation");
                return;
            }

            var dialog = CreateReservationDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is CreateReservationViewModel reservationViewModel)
            {
                await reservationViewModel.InitializeAsync(e);
            }

            var result = dialog.ShowDialog();
            if (result == true)
            {
                MessageBox.Show($"Reservation created for {e.DisplayName}.", "Reservation Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
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
