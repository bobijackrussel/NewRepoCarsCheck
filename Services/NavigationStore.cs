using System;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Services
{
    public class NavigationStore
    {
        private BaseViewModel? _currentViewModel;

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (ReferenceEquals(_currentViewModel, value))
                {
                    return;
                }

                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke(this, _currentViewModel);
            }
        }

        public event EventHandler<BaseViewModel?>? CurrentViewModelChanged;
    }
}
