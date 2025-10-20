using CarRentalManagment.Utilities.Theming;

namespace CarRentalManagment.Utilities.Configuration
{
    public class UserPreferences
    {
        public AppTheme Theme { get; set; } = AppTheme.Light;
        public string Language { get; set; } = "en-US";
    }
}
