using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CarRentalManagment.Services;

namespace CarRentalManagment.ViewModels
{
    public class AdminDiscountManagementViewModel : SectionViewModel
    {
        private readonly ILocalizationService _localizationService;

        public AdminDiscountManagementViewModel(ILocalizationService localizationService)
            : base(string.Empty, string.Empty)
        {
            _localizationService = localizationService;
            Discounts = new ObservableCollection<DiscountRow>();

            UpdateLocalizedText();
            _localizationService.LanguageChanged += OnLanguageChanged;

            SeedSampleDiscounts();
        }

        public ObservableCollection<DiscountRow> Discounts { get; }

        private void UpdateLocalizedText()
        {
            Title = _localizationService.GetString("AdminDiscounts_Title");
            Description = _localizationService.GetString("AdminDiscounts_Description");
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            UpdateLocalizedText();
            foreach (var discount in Discounts)
            {
                discount.Refresh();
            }
        }

        private void SeedSampleDiscounts()
        {
            Discounts.Clear();
            Discounts.Add(new DiscountRow("Weekend Escape", 15, DateTime.Today.AddDays(-7), DateTime.Today.AddDays(21)));
            Discounts.Add(new DiscountRow("Electric Fleet Launch", 10, DateTime.Today, DateTime.Today.AddDays(45)));
            Discounts.Add(new DiscountRow("Early Bird Summer", 20, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1)));
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public class DiscountRow : BaseViewModel
        {
            public DiscountRow(string name, int percentage, DateTime startsOn, DateTime endsOn)
            {
                Name = name;
                Percentage = percentage;
                StartsOn = startsOn;
                EndsOn = endsOn;
            }

            public string Name { get; }
            public int Percentage { get; }
            public DateTime StartsOn { get; }
            public DateTime EndsOn { get; }

            public string PercentageDisplay => $"{Percentage}%";
            public string ScheduleDisplay => $"{StartsOn:d} - {EndsOn:d}";

            public bool IsActive => DateTime.Today >= StartsOn && DateTime.Today <= EndsOn;

            public void Refresh()
            {
                OnPropertyChanged(nameof(PercentageDisplay));
                OnPropertyChanged(nameof(ScheduleDisplay));
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }
}
