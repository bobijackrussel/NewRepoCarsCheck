using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IUserSession _userSession;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;

        public LoginViewModel(
            IAuthService authService,
            INavigationService navigationService,
            IUserSession userSession)
        {
            _authService = authService;
            _navigationService = navigationService;
            _userSession = userSession;

            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin());
            NavigateToSignUpCommand = new RelayCommand(_ => NavigateToSignUp());
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

        public ICommand LoginCommand { get; }
        public ICommand NavigateToSignUpCommand { get; }

        private async Task LoginAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting login for email: {Email.Trim()}");
                var user = await _authService.LoginAsync(Email.Trim(), Password, CancellationToken.None);

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("Login failed - user is null");
                    ErrorMessage = "Invalid email or password.";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Login successful for user: {user.Email}");
                _userSession.CurrentUser = user;
                _navigationService.NavigateTo<MainViewModel>();
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void NavigateToSignUp()
        {
            _navigationService.NavigateTo<SignupViewModel>();
        }

        private bool CanLogin()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ClearForm()
        {
            Email = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
        }

        private void RaiseCanExecuteChanged()
        {
            if (LoginCommand is RelayCommand loginRelay)
            {
                loginRelay.RaiseCanExecuteChanged();
            }
        }
    }
}
