using System.Windows;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class EditVehicleDialog : Window
    {
        public EditVehicleDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is EditVehicleViewModel oldViewModel)
            {
                oldViewModel.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is EditVehicleViewModel newViewModel)
            {
                newViewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, DialogCloseRequestedEventArgs e)
        {
            DialogResult = e.DialogResult;
            Close();
        }
    }
}
