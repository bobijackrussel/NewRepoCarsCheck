using System;
using System.Collections.Concurrent;
using CarRentalManagment.ViewModels;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.Services
{
    public class NavigationService : INavigationService
    {
        private readonly ConcurrentDictionary<Type, Func<BaseViewModel>> _factories = new();
        private readonly NavigationStore _navigationStore;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(NavigationStore navigationStore, ILogger<NavigationService> logger)
        {
            _navigationStore = navigationStore;
            _logger = logger;
            _navigationStore.CurrentViewModelChanged += OnStoreCurrentViewModelChanged;
        }

        public BaseViewModel? CurrentViewModel => _navigationStore.CurrentViewModel;

        public event EventHandler<BaseViewModel?>? CurrentViewModelChanged;

        public void Register<TViewModel>(Func<TViewModel> factory) where TViewModel : BaseViewModel
        {
            var type = typeof(TViewModel);
            _factories[type] = () => factory();
        }

        public bool Navigate(BaseViewModel viewModel, bool disposePrevious = false)
        {
            if (viewModel == null)
            {
                return false;
            }

            UpdateCurrentViewModel(viewModel, disposePrevious);
            return true;
        }

        public bool NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var type = typeof(TViewModel);
            if (!_factories.TryGetValue(type, out var factory))
            {
                _logger.LogWarning("No view model registered for type {ViewModelType}", type.Name);
                return false;
            }

            var viewModel = factory();
            UpdateCurrentViewModel(viewModel, disposePrevious: true);
            return true;
        }

        private void UpdateCurrentViewModel(BaseViewModel viewModel, bool disposePrevious)
        {
            var previousViewModel = _navigationStore.CurrentViewModel;
            if (ReferenceEquals(previousViewModel, viewModel))
            {
                return;
            }

            if (disposePrevious)
            {
                previousViewModel?.Dispose();
            }

            _navigationStore.CurrentViewModel = viewModel;
        }

        private void OnStoreCurrentViewModelChanged(object? sender, BaseViewModel? viewModel)
        {
            CurrentViewModelChanged?.Invoke(this, viewModel);
        }
    }
}
