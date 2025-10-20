using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Utilities.Configuration;

namespace CarRentalManagment.Services
{
    public class SettingsService : ISettingsService, IDisposable
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _settingsPath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private UserPreferences _currentPreferences = new();
        private bool _isLoaded;
        private bool _disposed;

        public SettingsService()
        {
            var appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CarRentalManagment");

            if (!Directory.Exists(appDirectory))
            {
                Directory.CreateDirectory(appDirectory);
            }

            _settingsPath = Path.Combine(appDirectory, "userpreferences.json");
        }

        public UserPreferences CurrentPreferences => _currentPreferences;

        public event EventHandler<UserPreferences>? PreferencesChanged;

        public async Task<UserPreferences> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_isLoaded)
                {
                    return _currentPreferences;
                }

                if (File.Exists(_settingsPath))
                {
                    await using var stream = File.OpenRead(_settingsPath);
                    var preferences = await JsonSerializer.DeserializeAsync<UserPreferences>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
                    _currentPreferences = preferences ?? new UserPreferences();
                }
                else
                {
                    _currentPreferences = new UserPreferences();
                    await SaveInternalAsync(_currentPreferences, cancellationToken).ConfigureAwait(false);
                }

                _isLoaded = true;
                return _currentPreferences;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await SaveInternalAsync(preferences, cancellationToken).ConfigureAwait(false);
                _currentPreferences = preferences;
                _isLoaded = true;
                PreferencesChanged?.Invoke(this, _currentPreferences);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveInternalAsync(UserPreferences preferences, CancellationToken cancellationToken)
        {
            await using var stream = File.Create(_settingsPath);
            await JsonSerializer.SerializeAsync(stream, preferences, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _semaphore.Dispose();
            _disposed = true;
        }
    }
}
