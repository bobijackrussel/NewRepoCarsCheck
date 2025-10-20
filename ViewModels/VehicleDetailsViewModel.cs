using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class VehicleDetailsViewModel : BaseViewModel
    {
        private readonly IVehiclePhotoService _vehiclePhotoService;
        private readonly ILogger<VehicleDetailsViewModel> _logger;
        private readonly RelayCommand _reserveCommand;
        private readonly RelayCommand _backCommand;

        private VehicleCardViewModel? _vehicle;
        private VehiclePhotoItemViewModel? _selectedPhoto;
        private bool _isLoading;
        private string? _errorMessage;
        private CancellationTokenSource? _photosCts;

        public VehicleDetailsViewModel(IVehiclePhotoService vehiclePhotoService, ILogger<VehicleDetailsViewModel> logger)
        {
            _vehiclePhotoService = vehiclePhotoService;
            _logger = logger;

            Photos = new ObservableCollection<VehiclePhotoItemViewModel>();
            _reserveCommand = new RelayCommand(_ => OnReserveRequested(), _ => Vehicle != null);
            _backCommand = new RelayCommand(_ => OnBackRequested());
        }

        public VehicleCardViewModel? Vehicle
        {
            get => _vehicle;
            private set
            {
                if (SetProperty(ref _vehicle, value))
                {
                    _reserveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(HasVehicle));
                    OnPropertyChanged(nameof(SelectedPhotoUrl));
                }
            }
        }

        public bool HasVehicle => Vehicle != null;

        public ObservableCollection<VehiclePhotoItemViewModel> Photos { get; }

        public VehiclePhotoItemViewModel? SelectedPhoto
        {
            get => _selectedPhoto;
            set
            {
                if (SetProperty(ref _selectedPhoto, value))
                {
                    OnPropertyChanged(nameof(SelectedPhotoUrl));
                }
            }
        }

        public string? SelectedPhotoUrl => SelectedPhoto?.Url ?? Vehicle?.PrimaryPhotoUrl;

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand ReserveCommand => _reserveCommand;

        public ICommand BackCommand => _backCommand;

        public event EventHandler<VehicleCardViewModel?>? ReserveRequested;

        public event EventHandler? BackRequested;

        public async Task SetVehicleAsync(VehicleCardViewModel? vehicle, CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine("=== VehicleDetailsViewModel.SetVehicleAsync called ===");
            System.Diagnostics.Debug.WriteLine($"Vehicle: {vehicle?.DisplayName ?? "NULL"}");

            _photosCts?.Cancel();
            _photosCts?.Dispose();
            _photosCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var token = _photosCts.Token;

            Vehicle = vehicle;
            System.Diagnostics.Debug.WriteLine($"Vehicle property set. HasVehicle: {HasVehicle}");
            Photos.Clear();
            SelectedPhoto = null;
            ErrorMessage = null;

            if (vehicle == null)
            {
                System.Diagnostics.Debug.WriteLine("Vehicle is null, returning");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Loading photos for vehicle ID: {vehicle.Id}");

            try
            {
                IsLoading = true;
                var photos = await _vehiclePhotoService.GetByVehicleIdAsync(vehicle.Id, token);

                if (photos.Count == 0 && !string.IsNullOrWhiteSpace(vehicle.PrimaryPhotoUrl))
                {
                    Photos.Add(new VehiclePhotoItemViewModel(vehicle.PrimaryPhotoUrl!, vehicle.PrimaryPhotoCaption, true));
                }
                else
                {
                    foreach (var photo in photos)
                    {
                        Photos.Add(new VehiclePhotoItemViewModel(photo.PhotoUrl, photo.Caption, photo.IsPrimary));
                    }
                }

                SelectedPhoto = Photos.FirstOrDefault(p => p.IsPrimary) ?? Photos.FirstOrDefault();
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load photos for vehicle {VehicleId}", vehicle.Id);
                ErrorMessage = "We couldn't load photos for this vehicle.";
            }
            finally
            {
                IsLoading = false;
                _photosCts?.Dispose();
                _photosCts = null;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _photosCts?.Cancel();
            _photosCts?.Dispose();
            _photosCts = null;
        }

        private void OnReserveRequested()
        {
            ReserveRequested?.Invoke(this, Vehicle);
        }

        private void OnBackRequested()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public class VehiclePhotoItemViewModel
    {
        public VehiclePhotoItemViewModel(string url, string? caption, bool isPrimary)
        {
            Url = url;
            Caption = caption;
            IsPrimary = isPrimary;
        }

        public string Url { get; }

        public string? Caption { get; }

        public bool IsPrimary { get; }
    }
}
