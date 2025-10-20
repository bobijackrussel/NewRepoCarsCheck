using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class ViolationReportView : UserControl
    {
        public ViolationReportView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportViolationViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ReportViolationViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private static async Task InitializeAsync(ReportViolationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
