using System;
using System.Linq;
using System.Windows;
using CarRentalManagment.Utilities.Theming;

namespace CarRentalManagment.Services
{
    public class ThemeService : IThemeService
    {
        private static readonly Uri LightThemeUri = new("Resources/Themes/LightTheme.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new("Resources/Themes/DarkTheme.xaml", UriKind.Relative);

        private readonly Application _application;
        private AppTheme _currentTheme = AppTheme.Light;
        private ResourceDictionary? _currentDictionary;

        public ThemeService()
        {
            _application = Application.Current ?? throw new InvalidOperationException("An application instance is required to manage themes.");
        }

        public AppTheme CurrentTheme => _currentTheme;

        public event EventHandler<AppTheme>? ThemeChanged;

        public void ApplyTheme(AppTheme theme)
        {
            if (_application.Dispatcher.CheckAccess())
            {
                ApplyThemeInternal(theme);
            }
            else
            {
                _application.Dispatcher.Invoke(() => ApplyThemeInternal(theme));
            }
        }

        private void ApplyThemeInternal(AppTheme theme)
        {
            if (_currentTheme == theme && _currentDictionary != null)
            {
                return;
            }

            var targetUri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;
            var dictionaries = _application.Resources.MergedDictionaries;

            var themeDictionary = dictionaries.FirstOrDefault(d => d.Source != null &&
                (d.Source.Equals(LightThemeUri) || d.Source.Equals(DarkThemeUri)));

            if (themeDictionary != null)
            {
                dictionaries.Remove(themeDictionary);
            }

            var newDictionary = new ResourceDictionary { Source = targetUri };
            dictionaries.Insert(0, newDictionary);

            _currentDictionary = newDictionary;
            _currentTheme = theme;
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }
}
