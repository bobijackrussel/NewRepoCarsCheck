using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.Utilities;
using CarRentalManagment.ViewModels;
using CarRentalManagment.Views;

namespace CarRentalManagment.Views.Admin
{
    public partial class AdminVehicleManagementView : UserControl
    {
        private AdminVehicleManagementViewModel? _viewModel;

        public AdminVehicleManagementView()
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
                DetachViewModel(_viewModel);
            }

            _viewModel = e.NewValue as AdminVehicleManagementViewModel;

            if (_viewModel != null && IsLoaded)
            {
                AttachViewModel(_viewModel);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                AttachViewModel(_viewModel);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                DetachViewModel(_viewModel);
            }
        }

        private void AttachViewModel(AdminVehicleManagementViewModel viewModel)
        {
            viewModel.AddVehicleRequested += OnAddVehicleRequested;
            viewModel.EditVehicleRequested += OnEditVehicleRequested;
            viewModel.DeleteVehicleRequested += OnDeleteVehicleRequested;
        }

        private void DetachViewModel(AdminVehicleManagementViewModel viewModel)
        {
            viewModel.AddVehicleRequested -= OnAddVehicleRequested;
            viewModel.EditVehicleRequested -= OnEditVehicleRequested;
            viewModel.DeleteVehicleRequested -= OnDeleteVehicleRequested;
        }

        private async void OnAddVehicleRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var dialog = CreateAddVehicleDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                await _viewModel.ReloadAsync();
            }
        }

        private async void OnEditVehicleRequested(object? sender, AdminVehicleManagementViewModel.VehicleRow e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var dialog = CreateEditVehicleDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is EditVehicleViewModel viewModel)
            {
                await viewModel.InitializeAsync(e.Entity);
            }

            if (dialog.ShowDialog() == true)
            {
                await _viewModel.ReloadAsync();
            }
        }

        private async void OnDeleteVehicleRequested(object? sender, AdminVehicleManagementViewModel.VehicleRow e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
            MessageBoxResult result;

            if (owner != null)
            {
                result = MessageBox.Show(
                    owner,
                    $"Are you sure you want to delete {e.Name}?",
                    "Delete vehicle",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }
            else
            {
                result = MessageBox.Show(
                    $"Are you sure you want to delete {e.Name}?",
                    "Delete vehicle",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteVehicleAsync(e);
            }
        }

        private AddVehicleDialog CreateAddVehicleDialog()
        {
            return new AddVehicleDialog
            {
                DataContext = AppServices.GetRequiredService<AddVehicleViewModel>()
            };
        }

        private EditVehicleDialog CreateEditVehicleDialog()
        {
            return new EditVehicleDialog
            {
                DataContext = AppServices.GetRequiredService<EditVehicleViewModel>()
            };
        }
    }
}
