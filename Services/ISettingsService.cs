using System;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Utilities.Configuration;

namespace CarRentalManagment.Services
{
    public interface ISettingsService
    {
        UserPreferences CurrentPreferences { get; }
        event EventHandler<UserPreferences>? PreferencesChanged;
        Task<UserPreferences> LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    }
}
