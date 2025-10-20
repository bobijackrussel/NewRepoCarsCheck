using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class AdminLocationManagementViewModel : SectionViewModel
    {
        private readonly ILocalizationService _localizationService;

        private readonly RelayCommand _addLocationCommand;
        private readonly RelayCommand _editLocationCommand;
        private readonly RelayCommand _deleteLocationCommand;
        private readonly RelayCommand _toggleStatusCommand;

        public AdminLocationManagementViewModel(ILocalizationService localizationService)
            : base(string.Empty, string.Empty)
        {
            _localizationService = localizationService;
            Locations = new ObservableCollection<LocationRow>();

            _addLocationCommand = new RelayCommand(_ => OnAddLocationRequested());
            _editLocationCommand = new RelayCommand(p => OnEditLocationRequested(p as LocationRow), p => p is LocationRow);
            _deleteLocationCommand = new RelayCommand(p => OnDeleteLocationRequested(p as LocationRow), p => p is LocationRow);
            _toggleStatusCommand = new RelayCommand(p => ToggleLocationStatus(p as LocationRow), p => p is LocationRow);

            UpdateLocalizedText();
            _localizationService.LanguageChanged += OnLanguageChanged;

            SeedSampleLocations();
        }

        public ObservableCollection<LocationRow> Locations { get; }

        public ICommand AddLocationCommand => _addLocationCommand;

        public ICommand EditLocationCommand => _editLocationCommand;

        public ICommand DeleteLocationCommand => _deleteLocationCommand;

        public ICommand ToggleLocationStatusCommand => _toggleStatusCommand;

        public event EventHandler? AddLocationRequested;

        public event EventHandler<LocationRow>? EditLocationRequested;

        public event EventHandler<LocationRow>? DeleteLocationRequested;

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
            Locations.Add(new LocationRow(Guid.NewGuid(), "Downtown Branch", "Belgrade, RS", 42, 86, true));
            Locations.Add(new LocationRow(Guid.NewGuid(), "Airport Hub", "Novi Sad, RS", 27, 92, true));
            Locations.Add(new LocationRow(Guid.NewGuid(), "City Center", "Niš, RS", 18, 74, true));
        }

        public void AddLocation(LocationFormData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var row = new LocationRow(Guid.NewGuid(), data.Name, data.Subtitle, data.FleetCount, data.UtilizationPercent, data.IsActive);
            Locations.Insert(0, row);
        }

        public void UpdateLocation(LocationFormData data)
        {
            if (data?.Id == null)
            {
                return;
            }

            var row = Locations.FirstOrDefault(l => l.Id == data.Id.Value);
            row?.Update(data);
        }

        public void RemoveLocation(LocationRow row)
        {
            if (row == null)
            {
                return;
            }

            Locations.Remove(row);
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void OnAddLocationRequested()
        {
            AddLocationRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditLocationRequested(LocationRow? row)
        {
            if (row == null)
            {
                return;
            }

            EditLocationRequested?.Invoke(this, row);
        }

        private void OnDeleteLocationRequested(LocationRow? row)
        {
            if (row == null)
            {
                return;
            }

            DeleteLocationRequested?.Invoke(this, row);
        }

        private void ToggleLocationStatus(LocationRow? row)
        {
            row?.ToggleStatus();
        }

        public class LocationRow : BaseViewModel
        {
            private string _name;
            private string _subtitle;
            private int _fleetCount;
            private int _utilizationPercent;
            private bool _isActive;

            public LocationRow(Guid id, string name, string subtitle, int fleetCount, int utilizationPercent, bool isActive)
            {
                Id = id;
                _name = name;
                _subtitle = subtitle;
                _fleetCount = fleetCount;
                _utilizationPercent = utilizationPercent;
                _isActive = isActive;
            }

            public Guid Id { get; }

            public string Name
            {
                get => _name;
                private set => SetProperty(ref _name, value);
            }

            public string Subtitle
            {
                get => _subtitle;
                private set => SetProperty(ref _subtitle, value);
            }

            public int FleetCount
            {
                get => _fleetCount;
                private set
                {
                    if (SetProperty(ref _fleetCount, value))
                    {
                        OnPropertyChanged(nameof(FleetDisplay));
                    }
                }
            }

            public int UtilizationPercent
            {
                get => _utilizationPercent;
                private set
                {
                    if (SetProperty(ref _utilizationPercent, value))
                    {
                        OnPropertyChanged(nameof(UtilizationDisplay));
                    }
                }
            }

            public bool IsActive
            {
                get => _isActive;
                private set
                {
                    if (SetProperty(ref _isActive, value))
                    {
                        OnPropertyChanged(nameof(StatusDisplay));
                        OnPropertyChanged(nameof(ToggleStatusAction));
                    }
                }
            }

            public string FleetDisplay => string.Format(CultureInfo.CurrentCulture, "{0} vehicles", FleetCount);

            public string UtilizationDisplay => string.Format(CultureInfo.CurrentCulture, "{0}%", UtilizationPercent);

            public string StatusDisplay => IsActive ? "Active" : "Inactive";

            public string ToggleStatusAction => IsActive ? "Deactivate" : "Activate";

            public void Refresh()
            {
                OnPropertyChanged(nameof(UtilizationDisplay));
                OnPropertyChanged(nameof(FleetDisplay));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(ToggleStatusAction));
            }

            public void ToggleStatus()
            {
                IsActive = !IsActive;
            }

            public void Update(LocationFormData data)
            {
                if (data == null)
                {
                    return;
                }

                Name = data.Name;
                Subtitle = data.Subtitle;
                FleetCount = data.FleetCount;
                UtilizationPercent = data.UtilizationPercent;
                IsActive = data.IsActive;
            }
        }
    }
}
