using System;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public class UserSession : IUserSession
    {
        private User? _currentUser;

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (!Equals(_currentUser, value))
                {
                    _currentUser = value;
                    CurrentUserChanged?.Invoke(this, _currentUser);
                }
            }
        }

        public event EventHandler<User?>? CurrentUserChanged;
    }
}
