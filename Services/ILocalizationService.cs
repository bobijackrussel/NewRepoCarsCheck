using System;
using System.Globalization;

namespace CarRentalManagment.Services
{
    public interface ILocalizationService
    {
        CultureInfo CurrentCulture { get; }
        event EventHandler<CultureInfo>? LanguageChanged;
        void ApplyLanguage(string cultureName);

        /// <summary>
        /// Retrieves a localized string for the provided key.
        /// </summary>
        /// <param name="resourceKey">The resource identifier to look up.</param>
        /// <returns>The localized string if available; otherwise, the key itself.</returns>
        string GetString(string resourceKey);
    }
}
