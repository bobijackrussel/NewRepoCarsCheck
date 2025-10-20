using System;
using CarRentalManagment.Utilities.Theming;

namespace CarRentalManagment.Services
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        event EventHandler<AppTheme>? ThemeChanged;
        void ApplyTheme(AppTheme theme);
    }
}
