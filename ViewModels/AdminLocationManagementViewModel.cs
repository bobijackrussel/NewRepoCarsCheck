using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CarRentalManagment.Services;

namespace CarRentalManagment.ViewModels
{
    public class AdminLocationManagementViewModel : SectionViewModel
    {
        private readonly ILocalizationService _localizationService;

        public AdminLocationManagementViewModel(ILocalizationService localizationService)
            : base(string.Empty, string.Empty)
        {
            _localizationService = localizationService;
            Locations = new ObservableCollection<LocationRow>();

            UpdateLocalizedText();
            _localizationService.LanguageChanged += OnLanguageChanged;

            SeedSampleLocations();
        }

        public ObservableCollection<LocationRow> Locations { get; }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("AdminLocations_Title");
            Description = _localizationService.GetString("AdminLocations_Description");
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
            foreach (var location in Locations)
            {
                location.Refresh();
            }
        }

        private void SeedSampleLocations()
        {
            Locations.Clear();
            Locations.Add(new LocationRow("Downtown Branch", "Belgrade, RS", 42, 86));
            Locations.Add(new LocationRow("Airport Hub", "Novi Sad, RS", 27, 92));
            Locations.Add(new LocationRow("City Center", "Niš, RS", 18, 74));
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public class LocationRow : BaseViewModel
        {
            private string _subtitle;

            public LocationRow(string name, string subtitle, int fleetCount, int utilizationPercent)
            {
                Name = name;
                _subtitle = subtitle;
                FleetCount = fleetCount;
                UtilizationPercent = utilizationPercent;
            }

            public string Name { get; }

            public string Subtitle
            {
                get => _subtitle;
                private set => SetProperty(ref _subtitle, value);
            }

            public int FleetCount { get; }

            public int UtilizationPercent { get; }

            public string UtilizationDisplay => $"{UtilizationPercent}%";

            public void Refresh()
            {
                OnPropertyChanged(nameof(UtilizationDisplay));
            }
        }
    }
}
