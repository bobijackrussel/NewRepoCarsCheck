using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class LoginSignupViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IUserSession _userSession;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private bool _isLoginMode = true;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public LoginSignupViewModel(IAuthService authService, INavigationService navigationService, IUserSession userSession)
        {
            _authService = authService;
            _navigationService = navigationService;
            _userSession = userSession;

            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanAttemptLogin());
            SignUpCommand = new RelayCommand(async _ => await SignUpAsync(), _ => CanAttemptSignUp());
            ToggleModeCommand = new RelayCommand(_ => ToggleMode());
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

        public bool IsLoginMode
        {
            get => _isLoginMode;
            set
            {
                if (SetProperty(ref _isLoginMode, value))
                {
                    StatusMessage = string.Empty;
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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand ToggleModeCommand { get; }

        private async Task LoginAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var user = await _authService.LoginAsync(Email.Trim(), Password, CancellationToken.None);
                if (user == null)
                {
                    StatusMessage = "Invalid email or password.";
                    return;
                }

                _userSession.CurrentUser = user;
                _navigationService.NavigateTo<MainViewModel>();
                ClearForm();
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while logging in.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SignUpAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
                {
                    StatusMessage = "Passwords do not match.";
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
                    StatusMessage = "An account with this email already exists.";
                    return;
                }

                IsLoginMode = true;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                StatusMessage = "Account created! You can now log in.";
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred while creating the account.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ToggleMode()
        {
            IsLoginMode = !IsLoginMode;
            StatusMessage = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool CanAttemptLogin()
        {
            return IsLoginMode && !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanAttemptSignUp()
        {
            return !IsLoginMode && !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(FirstName);
        }

        private void ClearForm()
        {
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
        }

        private void RaiseCanExecuteChanged()
        {
            if (LoginCommand is RelayCommand loginRelay)
            {
                loginRelay.RaiseCanExecuteChanged();
            }

            if (SignUpCommand is RelayCommand signupRelay)
            {
                signupRelay.RaiseCanExecuteChanged();
            }
        }
    }
}
