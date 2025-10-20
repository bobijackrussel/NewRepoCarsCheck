using System;
using CarRentalManagment.Models;

namespace CarRentalManagment.Services
{
    public interface IUserSession
    {
        User? CurrentUser { get; set; }
        event EventHandler<User?>? CurrentUserChanged;
    }
}
