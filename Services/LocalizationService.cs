using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace CarRentalManagment.Services
{
    public class LocalizationService : ILocalizationService
    {
        private static readonly Uri DefaultDictionaryUri = new("Resources/Localization/Strings.en-US.xaml", UriKind.Relative);

        private readonly Application _application;
        private readonly Dictionary<string, string> _stringLookup = new(StringComparer.OrdinalIgnoreCase);

        private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en-US");
        private ResourceDictionary? _currentDictionary;

        public LocalizationService()
        {
            _application = Application.Current ?? throw new InvalidOperationException("An application instance is required to manage localization resources.");
            LoadLanguageResources(_currentCulture.Name);
        }

        public CultureInfo CurrentCulture => _currentCulture;

        public event EventHandler<CultureInfo>? LanguageChanged;

        public void ApplyLanguage(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                throw new ArgumentException("A culture name is required.", nameof(cultureName));
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);

            void Apply()
            {
                _currentCulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                LoadLanguageResources(culture.Name);

                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

                LanguageChanged?.Invoke(this, _currentCulture);
            }

            if (!_application.Dispatcher.CheckAccess())
            {
                _application.Dispatcher.Invoke(Apply);
            }
            else
            {
                Apply();
            }
        }

        public string GetString(string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return string.Empty;
            }

            if (_stringLookup.TryGetValue(resourceKey, out var value))
            {
                return value;
            }

            if (_application.Resources.Contains(resourceKey) && _application.Resources[resourceKey] is string resource)
            {
                return resource;
            }

            return resourceKey;
        }

        private void LoadLanguageResources(string cultureName)
        {
            var dictionaries = _application.Resources.MergedDictionaries;
            var dictionary = ResolveDictionary(cultureName);

            if (_currentDictionary != null)
            {
                dictionaries.Remove(_currentDictionary);
            }

            dictionaries.Add(dictionary);
            _currentDictionary = dictionary;

            _stringLookup.Clear();
            foreach (var entry in dictionary.Keys)
            {
                if (entry is string key && dictionary[entry] is string value)
                {
                    _stringLookup[key] = value;
                }
            }
        }

        private ResourceDictionary ResolveDictionary(string cultureName)
        {
            foreach (var candidate in EnumerateCandidates(cultureName))
            {
                var uri = new Uri($"Resources/Localization/Strings.{candidate}.xaml", UriKind.Relative);
                try
                {
                    return new ResourceDictionary { Source = uri };
                }
                catch (FileNotFoundException)
                {
                    // Continue with next candidate
                }
                catch (IOException)
                {
                    // Continue with next candidate
                }
                catch (XamlParseException)
                {
                    // Continue with next candidate
                }
                catch (UriFormatException)
                {
                    // Continue with next candidate
                }
            }

            return new ResourceDictionary { Source = DefaultDictionaryUri };
        }

        private static IEnumerable<string> EnumerateCandidates(string cultureName)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(cultureName))
            {
                foreach (var candidate in EnumerateFromCulture(cultureName))
                {
                    if (seen.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }

            if (seen.Add("en-US"))
            {
                yield return "en-US";
            }
        }

        private static IEnumerable<string> EnumerateFromCulture(string cultureName)
        {
            CultureInfo? culture = null;

            try
            {
                culture = CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                yield break;
            }

            var current = culture;
            while (current != null && current != CultureInfo.InvariantCulture)
            {
                yield return current.Name;
                current = current.Parent;
            }

            var twoLetter = culture.TwoLetterISOLanguageName;
            if (!string.IsNullOrWhiteSpace(twoLetter))
            {
                yield return twoLetter;
            }
        }
    }
}
