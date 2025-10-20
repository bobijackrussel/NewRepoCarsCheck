using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using CarRentalManagment.Utilities.Configuration;
using CarRentalManagment.Utilities.Theming;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class SettingsViewModel : SectionViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localizationService;
        private readonly IUserSession _userSession;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly RelayCommand _saveCommand;

        private readonly ObservableCollection<LanguageOption> _languages;

        private bool _isInitialized;
        private bool _isSaving;
        private bool _isDarkTheme;
        private AppTheme _selectedTheme = AppTheme.Light;
        private LanguageOption? _selectedLanguage;
        private string? _statusMessage;
        private string? _errorMessage;
        private string? _displayName;
        private string? _email;
        private string? _username;
        private bool _hasUnsavedChanges;
        private bool _isApplyingTheme;
        private bool _isUpdatingLanguages;
        private string _loadedLanguage = "en-US";
        private AppTheme _loadedTheme = AppTheme.Light;

        public SettingsViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            ILocalizationService localizationService,
            IUserSession userSession,
            ILogger<SettingsViewModel> logger)
            : base(string.Empty, string.Empty)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            _localizationService = localizationService;
            _userSession = userSession;
            _logger = logger;

            _languages = new ObservableCollection<LanguageOption>();

            _saveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave);

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
            UpdateProfile(_userSession.CurrentUser);

            UpdateLocalizedText();
            PopulateLanguages();
        }

        public ObservableCollection<LanguageOption> Languages => _languages;

        public ICommand SaveCommand => _saveCommand;

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    _saveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value) && !_isApplyingTheme)
                {
                    SelectedTheme = value ? AppTheme.Dark : AppTheme.Light;
                }
            }
        }

        public AppTheme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    try
                    {
                        _isApplyingTheme = true;
                        IsDarkTheme = value == AppTheme.Dark;
                    }
                    finally
                    {
                        _isApplyingTheme = false;
                    }

                    _themeService.ApplyTheme(value);
                    UpdateUnsavedChanges();
                }
            }
        }

        public LanguageOption? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    if (value != null && !_isUpdatingLanguages)
                    {
                        _localizationService.ApplyLanguage(value.CultureName);
                    }

                    UpdateUnsavedChanges();
                }
            }
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string? DisplayName
        {
            get => _displayName;
            private set => SetProperty(ref _displayName, value);
        }

        public string? Email
        {
            get => _email;
            private set => SetProperty(ref _email, value);
        }

        public string? Username
        {
            get => _username;
            private set => SetProperty(ref _username, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    _saveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool CanSave => !IsSaving && HasUnsavedChanges;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            try
            {
                var preferences = await _settingsService.LoadAsync().ConfigureAwait(true);

                _loadedTheme = preferences.Theme;
                _loadedLanguage = preferences.Language;

                SelectedTheme = preferences.Theme;
                SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == preferences.Language) ?? Languages.First();

                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user preferences.");
                ErrorMessage = "We couldn't load your saved preferences. Default settings are being used.";
                SelectedTheme = AppTheme.Light;
                SelectedLanguage = Languages.First();
            }
        }

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            IsSaving = true;
            ErrorMessage = null;
            StatusMessage = null;

            try
            {
                var preferences = new UserPreferences
                {
                    Theme = SelectedTheme,
                    Language = SelectedLanguage?.CultureName ?? _loadedLanguage
                };

                await _settingsService.SaveAsync(preferences).ConfigureAwait(true);

                _loadedTheme = preferences.Theme;
                _loadedLanguage = preferences.Language;
                HasUnsavedChanges = false;

                StatusMessage = "Settings saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user preferences.");
                ErrorMessage = "Saving your settings failed. Please try again.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void OnLanguageChanged(object? sender, CultureInfo culture)
        {
            UpdateLocalizedText();
            PopulateLanguages();
        }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("Settings_Title");
            Description = _localizationService.GetString("Settings_Description");
        }

        private void PopulateLanguages()
        {
            var previouslySelected = SelectedLanguage?.CultureName ?? _loadedLanguage;

            var options = new[]
            {
                new LanguageOption(_localizationService.GetString("Language_English"), "en-US"),
                new LanguageOption(_localizationService.GetString("Language_Serbian"), "sr-RS")
            };

            _isUpdatingLanguages = true;
            try
            {
                _languages.Clear();
                foreach (var option in options)
                {
                    _languages.Add(option);
                }

                var match = _languages.FirstOrDefault(l =>
                    string.Equals(l.CultureName, previouslySelected, StringComparison.OrdinalIgnoreCase));

                SelectedLanguage = match ?? _languages.FirstOrDefault();
            }
            finally
            {
                _isUpdatingLanguages = false;
            }
        }

        private void UpdateProfile(User? user)
        {
            DisplayName = string.IsNullOrWhiteSpace(user?.FullName) ? "Guest" : user!.FullName;
            Email = user?.Email ?? "Not provided";
            Username = user?.Username ?? "-";
        }

        private void OnCurrentUserChanged(object? sender, User? user)
        {
            UpdateProfile(user);
        }

        private void UpdateUnsavedChanges()
        {
            var language = SelectedLanguage?.CultureName ?? _loadedLanguage;
            var hasChanges = language != _loadedLanguage || SelectedTheme != _loadedTheme;
            HasUnsavedChanges = hasChanges;

            if (hasChanges)
            {
                StatusMessage = null;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public record LanguageOption(string DisplayName, string CultureName)
        {
            public override string ToString() => DisplayName;
        }
    }
}

