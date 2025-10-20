using System.Windows;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views.Admin
{
    public partial class LocationEditorDialog : Window
    {
        public LocationEditorDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LocationEditorViewModel oldViewModel)
            {
                oldViewModel.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is LocationEditorViewModel newViewModel)
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
