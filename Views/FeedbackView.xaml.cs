using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class FeedbackView : UserControl
    {
        public FeedbackView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FeedbackViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FeedbackViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private static async Task InitializeAsync(FeedbackViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
