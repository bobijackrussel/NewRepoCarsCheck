using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.Utilities;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views.Admin
{
    public partial class AdminLocationManagementView : UserControl
    {
        private AdminLocationManagementViewModel? _viewModel;

        public AdminLocationManagementView()
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

            _viewModel = e.NewValue as AdminLocationManagementViewModel;

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

        private void AttachViewModel(AdminLocationManagementViewModel viewModel)
        {
            viewModel.AddLocationRequested += OnAddLocationRequested;
            viewModel.EditLocationRequested += OnEditLocationRequested;
            viewModel.DeleteLocationRequested += OnDeleteLocationRequested;
        }

        private void DetachViewModel(AdminLocationManagementViewModel viewModel)
        {
            viewModel.AddLocationRequested -= OnAddLocationRequested;
            viewModel.EditLocationRequested -= OnEditLocationRequested;
            viewModel.DeleteLocationRequested -= OnDeleteLocationRequested;
        }

        private async void OnAddLocationRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var dialog = CreateLocationDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is LocationEditorViewModel viewModel)
            {
                await viewModel.InitializeAsync(null);
            }

            if (dialog.ShowDialog() == true && dialog.DataContext is LocationEditorViewModel vm && vm.Result != null)
            {
                _viewModel.AddLocation(vm.Result);
            }
        }

        private async void OnEditLocationRequested(object? sender, AdminLocationManagementViewModel.LocationRow e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var dialog = CreateLocationDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is LocationEditorViewModel viewModel)
            {
                await viewModel.InitializeAsync(e);
            }

            if (dialog.ShowDialog() == true && dialog.DataContext is LocationEditorViewModel vm && vm.Result != null)
            {
                _viewModel.UpdateLocation(vm.Result);
            }
        }

        private void OnDeleteLocationRequested(object? sender, AdminLocationManagementViewModel.LocationRow e)
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
                    $"Delete {e.Name}?",
                    "Delete location",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }
            else
            {
                result = MessageBox.Show(
                    $"Delete {e.Name}?",
                    "Delete location",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.RemoveLocation(e);
            }
        }

        private LocationEditorDialog CreateLocationDialog()
        {
            return new LocationEditorDialog
            {
                DataContext = AppServices.GetRequiredService<LocationEditorViewModel>()
            };
        }
    }
}
