using CarRentalManagment.Services;

namespace CarRentalManagment.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private BaseViewModel? _currentViewModel;

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;
            _currentViewModel = _navigationService.CurrentViewModel;
        }

        public string ApplicationTitle => "Car Rental Management";

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public override void Dispose()
        {
            _navigationService.CurrentViewModelChanged -= OnCurrentViewModelChanged;
            base.Dispose();
        }

        private void OnCurrentViewModelChanged(object? sender, BaseViewModel? viewModel)
        {
            CurrentViewModel = viewModel;
        }
    }
}
