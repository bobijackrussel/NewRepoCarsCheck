using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class LocationEditorViewModel : BaseViewModel
    {
        private readonly RelayCommand _saveCommand;
        private readonly RelayCommand _cancelCommand;

        private Guid? _locationId;
        private string _name = string.Empty;
        private string _subtitle = string.Empty;
        private string _fleetCount = "0";
        private string _utilization = "0";
        private bool _isActive = true;
        private bool _isSaving;
        private string? _errorMessage;
        private bool _isEdit;

        public LocationEditorViewModel()
        {
            _saveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            _cancelCommand = new RelayCommand(_ => RequestClose(false));
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        public string FleetCount
        {
            get => _fleetCount;
            set => SetProperty(ref _fleetCount, value);
        }

        public string Utilization
        {
            get => _utilization;
            set => SetProperty(ref _utilization, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

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

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string Title => _isEdit ? "Edit location" : "Add location";

        public string Description => _isEdit
            ? "Update the details for this rental branch."
            : "Provide the information for the new rental branch.";

        public string PrimaryButtonText => _isEdit ? "Update location" : "Add location";

        public LocationFormData? Result { get; private set; }

        public ICommand SaveCommand => _saveCommand;

        public ICommand CancelCommand => _cancelCommand;

        public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

        public Task InitializeAsync(AdminLocationManagementViewModel.LocationRow? row)
        {
            if (row == null)
            {
                _isEdit = false;
                _locationId = null;
                Name = string.Empty;
                Subtitle = string.Empty;
                FleetCount = "0";
                Utilization = "0";
                IsActive = true;
            }
            else
            {
                _isEdit = true;
                _locationId = row.Id;
                Name = row.Name;
                Subtitle = row.Subtitle;
                FleetCount = row.FleetCount.ToString(CultureInfo.InvariantCulture);
                Utilization = row.UtilizationPercent.ToString(CultureInfo.InvariantCulture);
                IsActive = row.IsActive;
            }

            ErrorMessage = null;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(PrimaryButtonText));

            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            ErrorMessage = null;

            var trimmedName = Name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ErrorMessage = "Location name is required.";
                return;
            }

            if (!int.TryParse(FleetCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fleet) || fleet < 0)
            {
                ErrorMessage = "Fleet size must be a positive number.";
                return;
            }

            if (!int.TryParse(Utilization, NumberStyles.Integer, CultureInfo.InvariantCulture, out var utilization) || utilization < 0 || utilization > 100)
            {
                ErrorMessage = "Utilization must be between 0 and 100.";
                return;
            }

            try
            {
                IsSaving = true;

                Result = new LocationFormData
                {
                    Id = _locationId,
                    Name = trimmedName!,
                    Subtitle = Subtitle?.Trim() ?? string.Empty,
                    FleetCount = fleet,
                    UtilizationPercent = utilization,
                    IsActive = IsActive
                };

                RequestClose(true);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void RequestClose(bool dialogResult)
        {
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(dialogResult));
        }
    }
}
