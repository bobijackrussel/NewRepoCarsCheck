using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class SignupViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private string _successMessage = string.Empty;

        public SignupViewModel(
            IAuthService authService,
            INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;

            SignUpCommand = new RelayCommand(async _ => await SignUpAsync(), _ => CanSignUp());
            NavigateToLoginCommand = new RelayCommand(_ => NavigateToLogin());
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public ICommand SignUpCommand { get; }
        public ICommand NavigateToLoginCommand { get; }

        private async Task SignUpAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                // Validate passwords match
                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
                {
                    ErrorMessage = "Passwords do not match.";
                    return;
                }

                // Validate password strength
                if (Password.Length < 6)
                {
                    ErrorMessage = "Password must be at least 6 characters long.";
                    return;
                }

                var newUser = new User
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim()
                };

                var registered = await _authService.SignUpAsync(newUser, Password, CancellationToken.None);
                if (!registered)
                {
                    ErrorMessage = "An account with this email already exists.";
                    return;
                }

                SuccessMessage = "Account created successfully! Redirecting to login...";

                // Wait a moment to show the success message
                await Task.Delay(1500);

                // Navigate to login
                _navigationService.NavigateTo<LoginViewModel>();
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while creating the account. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Signup error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void NavigateToLogin()
        {
            _navigationService.NavigateTo<LoginViewModel>();
        }

        private bool CanSignUp()
        {
            return !IsBusy
                && !string.IsNullOrWhiteSpace(Email)
                && !string.IsNullOrWhiteSpace(Password)
                && !string.IsNullOrWhiteSpace(ConfirmPassword)
                && !string.IsNullOrWhiteSpace(FirstName);
        }

        private void RaiseCanExecuteChanged()
        {
            if (SignUpCommand is RelayCommand signupRelay)
            {
                signupRelay.RaiseCanExecuteChanged();
            }
        }
    }
}
