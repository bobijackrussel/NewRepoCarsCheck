using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SettingsViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private static async Task InitializeAsync(SettingsViewModel viewModel)
        {
            await viewModel.InitializeAsync().ConfigureAwait(true);
        }
    }
}
