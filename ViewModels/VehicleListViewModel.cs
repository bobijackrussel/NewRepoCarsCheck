using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using CarRentalManagment.Utilities.SampleData;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class VehicleListViewModel : BaseViewModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IVehiclePhotoService _vehiclePhotoService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<VehicleListViewModel> _logger;

        private readonly ICollectionView _vehiclesView;
        private readonly RelayCommand _refreshCommand;
        private readonly RelayCommand _clearSearchCommand;
        private readonly RelayCommand _selectVehicleCommand;
        private RelayCommand? _showDetailsCommand;

        private VehicleCardViewModel? _selectedVehicle;
        private string _searchText = string.Empty;
        private VehicleSortOption? _selectedSortOption;
        private string _selectedVehicleType = string.Empty;
        private string _selectedTransmission = string.Empty;
        private PriceRangeOption? _selectedPriceRange;
        private bool _isLoading;
        private string? _errorMessage;
        private CancellationTokenSource? _loadingCts;

        private INavigationService? _navigationService;
        private VehicleDetailsViewModel? _vehicleDetailsViewModel;

        private string _allTypesOption = string.Empty;
        private string _allTransmissionsOption = string.Empty;
        private string _allPricesOption = string.Empty;

        public VehicleListViewModel(
            IVehicleService vehicleService,
            IVehiclePhotoService vehiclePhotoService,
            ILocalizationService localizationService,
            ILogger<VehicleListViewModel> logger)
        {
            _vehicleService = vehicleService;
            _vehiclePhotoService = vehiclePhotoService;
            _localizationService = localizationService;
            _logger = logger;

            Vehicles = new ObservableCollection<VehicleCardViewModel>();
            _vehiclesView = CollectionViewSource.GetDefaultView(Vehicles);
            _vehiclesView.Filter = FilterVehicles;
            System.Diagnostics.Debug.WriteLine("VehicleListViewModel constructor - Vehicles collection initialized");

            _localizationService.LanguageChanged += OnLanguageChanged;

            UpdateLocalization(isInitial: true);

            _refreshCommand = new RelayCommand(async _ => await LoadVehiclesAsync(), _ => !IsLoading);
            _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !IsLoading && !string.IsNullOrWhiteSpace(SearchText));
            _selectVehicleCommand = new RelayCommand(
                parameter =>
                {
                    System.Diagnostics.Debug.WriteLine("=== SelectVehicleCommand EXECUTE method called ===");
                    System.Diagnostics.Debug.WriteLine($"Parameter type: {parameter?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"Parameter value: {parameter}");

                    if (parameter is VehicleCardViewModel card)
                    {
                        System.Diagnostics.Debug.WriteLine($"Parameter is VehicleCardViewModel: {card.DisplayName}");
                        SelectedVehicle = card;
                        System.Diagnostics.Debug.WriteLine($"SelectedVehicle set to: {SelectedVehicle?.DisplayName}");
                        VehicleDetailsRequested?.Invoke(this, card);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Parameter is NOT VehicleCardViewModel!");
                    }
                },
                parameter =>
                {
                    System.Diagnostics.Debug.WriteLine("=== SelectVehicleCommand CANEXECUTE method called ===");
                    System.Diagnostics.Debug.WriteLine($"Parameter type: {parameter?.GetType().Name ?? "null"}");
                    bool canExecute = parameter is VehicleCardViewModel;
                    System.Diagnostics.Debug.WriteLine($"CanExecute result: {canExecute}");
                    return canExecute;
                });
        }

        public ObservableCollection<VehicleCardViewModel> Vehicles { get; }

        public ICollectionView VehiclesView => _vehiclesView;

        public ObservableCollection<VehicleSortOption> SortOptions { get; } = new();

        public ObservableCollection<string> VehicleTypes { get; } = new();

        public ObservableCollection<string> TransmissionOptions { get; } = new();

        public ObservableCollection<PriceRangeOption> PriceRanges { get; } = new();

        public VehicleSortOption? SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplySortDescriptions();
                }
            }
        }

        public string SelectedVehicleType
        {
            get => _selectedVehicleType;
            set
            {
                if (SetProperty(ref _selectedVehicleType, value))
                {
                    _vehiclesView.Refresh();
                }
            }
        }

        public string SelectedTransmission
        {
            get => _selectedTransmission;
            set
            {
                if (SetProperty(ref _selectedTransmission, value))
                {
                    _vehiclesView.Refresh();
                }
            }
        }

        public PriceRangeOption? SelectedPriceRange
        {
            get => _selectedPriceRange;
            set
            {
                if (SetProperty(ref _selectedPriceRange, value))
                {
                    _vehiclesView.Refresh();
                }
            }
        }

        public VehicleCardViewModel? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                System.Diagnostics.Debug.WriteLine($"=== SelectedVehicle property setter called with: {value?.DisplayName ?? "null"} ===");
                if (SetProperty(ref _selectedVehicle, value))
                {
                    System.Diagnostics.Debug.WriteLine($"Property changed, invoking SelectedVehicleChanged event");
                    SelectedVehicleChanged?.Invoke(this, value);
                    System.Diagnostics.Debug.WriteLine($"Event invoked. Listeners: {SelectedVehicleChanged?.GetInvocationList().Length ?? 0}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Property value unchanged, no event fired");
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _vehiclesView.Refresh();
                    _clearSearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    _refreshCommand.RaiseCanExecuteChanged();
                    _clearSearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RefreshCommand => _refreshCommand;

        public ICommand ClearSearchCommand => _clearSearchCommand;

        public ICommand SelectVehicleCommand => _selectVehicleCommand;

        public ICommand? ShowDetailsCommand => _showDetailsCommand;

        public event EventHandler<VehicleCardViewModel?>? SelectedVehicleChanged;

        public event EventHandler<VehicleCardViewModel?>? VehicleDetailsRequested;

        public void AttachNavigationService(INavigationService navigationService, VehicleDetailsViewModel vehicleDetailsViewModel)
        {
            _navigationService = navigationService;
            _vehicleDetailsViewModel = vehicleDetailsViewModel;

            _showDetailsCommand = new RelayCommand(
                parameter =>
                {
                    System.Diagnostics.Debug.WriteLine("=== ShowDetailsCommand EXECUTE method called ===");
                    System.Diagnostics.Debug.WriteLine($"Parameter type: {parameter?.GetType().Name ?? "null"}");

                    if (parameter is VehicleCardViewModel card && _navigationService != null && _vehicleDetailsViewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Setting vehicle details for: {card.DisplayName}");
                        SelectedVehicle = card;
                        _ = _vehicleDetailsViewModel.SetVehicleAsync(card);
                        _navigationService.Navigate(_vehicleDetailsViewModel, disposePrevious: false);
                    }
                },
                parameter => parameter is VehicleCardViewModel && _navigationService != null);

            OnPropertyChanged(nameof(ShowDetailsCommand));
        }

        private void UpdateLocalization(bool isInitial = false)
        {
            var previousTypes = _allTypesOption;
            var previousTransmissions = _allTransmissionsOption;
            var previousPrices = _allPricesOption;
            var previousSortKey = GetSortKey(SelectedSortOption);
            var previousPriceSelection = SelectedPriceRange;

            _allTypesOption = _localizationService.GetString("Vehicles_Filter_AllTypes");
            _allTransmissionsOption = _localizationService.GetString("Vehicles_Filter_AllTransmissions");
            _allPricesOption = _localizationService.GetString("Vehicles_Filter_AllPrices");

            UpdateSortOptions(previousSortKey);

            UpdateDefaultOption(VehicleTypes, previousTypes, _allTypesOption, () => SelectedVehicleType, value => SelectedVehicleType = value, isInitial);
            UpdateDefaultOption(TransmissionOptions, previousTransmissions, _allTransmissionsOption, () => SelectedTransmission, value => SelectedTransmission = value, isInitial);

            UpdatePriceRanges(previousPriceSelection);

            if (isInitial)
            {
                SelectedVehicleType = _allTypesOption;
                SelectedTransmission = _allTransmissionsOption;
            }

            _vehiclesView.Refresh();
        }

        private void UpdateSortOptions(string? previousSortKey)
        {
            var options = BuildSortOptions().ToList();

            SortOptions.Clear();
            foreach (var option in options)
            {
                SortOptions.Add(option);
            }

            SelectedSortOption = SortOptions.FirstOrDefault(o => GetSortKey(o) == previousSortKey) ?? SortOptions.FirstOrDefault();
        }

        private IEnumerable<VehicleSortOption> BuildSortOptions()
        {
            yield return new VehicleSortOption(
                _localizationService.GetString("Vehicles_Sort_Recommended"),
                new SortDescription(nameof(VehicleCardViewModel.DisplayName), ListSortDirection.Ascending));
            yield return new VehicleSortOption(
                _localizationService.GetString("Vehicles_Sort_DailyRateLowHigh"),
                new SortDescription(nameof(VehicleCardViewModel.DailyRate), ListSortDirection.Ascending));
            yield return new VehicleSortOption(
                _localizationService.GetString("Vehicles_Sort_DailyRateHighLow"),
                new SortDescription(nameof(VehicleCardViewModel.DailyRate), ListSortDirection.Descending));
            yield return new VehicleSortOption(
                _localizationService.GetString("Vehicles_Sort_Newest"),
                new SortDescription(nameof(VehicleCardViewModel.CreatedAt), ListSortDirection.Descending));
        }

        private static string GetSortKey(VehicleSortOption? option)
        {
            if (option == null)
            {
                return string.Empty;
            }

            return string.Join("|", option.SortDescriptions.Select(sd => $"{sd.PropertyName}:{(int)sd.Direction}"));
        }

        private void UpdateDefaultOption(
            ObservableCollection<string> target,
            string previousValue,
            string newValue,
            Func<string> getSelection,
            Action<string> setSelection,
            bool isInitial)
        {
            var selection = getSelection();
            var shouldSelectDefault = isInitial
                                       || string.IsNullOrWhiteSpace(selection)
                                       || string.Equals(selection, previousValue, StringComparison.OrdinalIgnoreCase);

            if (target.Count == 0)
            {
                target.Add(newValue);
            }
            else
            {
                var index = !string.IsNullOrEmpty(previousValue) ? target.IndexOf(previousValue) : 0;
                if (index < 0)
                {
                    index = 0;
                }

                if (index < target.Count)
                {
                    target[index] = newValue;
                }
                else
                {
                    target.Insert(0, newValue);
                }
            }

            if (shouldSelectDefault)
            {
                setSelection(newValue);
            }
        }

        private void UpdatePriceRanges(PriceRangeOption? previousSelection)
        {
            var ranges = BuildPriceRanges().ToList();

            PriceRanges.Clear();
            foreach (var range in ranges)
            {
                PriceRanges.Add(range);
            }

            if (previousSelection != null)
            {
                SelectedPriceRange = PriceRanges.FirstOrDefault(r => PriceRangeEquals(r, previousSelection));
            }

            SelectedPriceRange ??= PriceRanges.FirstOrDefault(r => r.IsDefault);
        }

        private IEnumerable<PriceRangeOption> BuildPriceRanges()
        {
            yield return new PriceRangeOption(_allPricesOption, 0m, null, isDefault: true);
            yield return new PriceRangeOption(_localizationService.GetString("Vehicles_PriceRange_0_50"), 0m, 50m);
            yield return new PriceRangeOption(_localizationService.GetString("Vehicles_PriceRange_51_100"), 51m, 100m);
            yield return new PriceRangeOption(_localizationService.GetString("Vehicles_PriceRange_101_200"), 101m, 200m);
            yield return new PriceRangeOption(_localizationService.GetString("Vehicles_PriceRange_201_Plus"), 201m, null);
        }

        private static bool PriceRangeEquals(PriceRangeOption left, PriceRangeOption right)
        {
            return left.Min == right.Min && Nullable.Equals(left.Max, right.Max);
        }

        private void OnLanguageChanged(object? sender, CultureInfo e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => UpdateLocalization());
            }
            else
            {
                UpdateLocalization();
            }
        }

        public async Task LoadVehiclesAsync(long? preferredSelectionId = null, CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine("=== LoadVehiclesAsync STARTED ===");
            _logger.LogInformation("LoadVehiclesAsync started");

            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var token = _loadingCts.Token;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                System.Diagnostics.Debug.WriteLine("Calling VehicleService.GetAllAsync");
                _logger.LogInformation("Calling VehicleService.GetAllAsync");

                var primaryPhotos = new Dictionary<long, VehiclePhoto?>();
                List<Vehicle> vehicleList;

                try
                {
                    var vehicles = await _vehicleService.GetAllAsync(token).ConfigureAwait(false);
                    vehicleList = vehicles.ToList();
                    System.Diagnostics.Debug.WriteLine($"Received {vehicleList.Count} vehicles from service");
                    _logger.LogInformation("Received {Count} vehicles from service", vehicleList.Count);

                    var ids = vehicleList.Select(v => v.Id).Where(id => id > 0).Distinct().ToList();
                    _logger.LogInformation("Extracted {IdCount} vehicle IDs", ids.Count);

                    if (ids.Count > 0)
                    {
                        try
                        {
                            _logger.LogInformation("Fetching primary photos for {IdCount} vehicles", ids.Count);
                            var photos = await _vehiclePhotoService.GetPrimaryPhotosAsync(ids, token).ConfigureAwait(false);
                            _logger.LogInformation("Received {PhotoCount} primary photos", photos?.Count ?? 0);
                            if (photos != null)
                            {
                                primaryPhotos = new Dictionary<long, VehiclePhoto?>(photos);
                            }
                        }
                        catch (Exception photoEx)
                        {
                            _logger.LogWarning(photoEx, "Failed to load vehicle photos; continuing without images.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load vehicles from the database.");
                    ErrorMessage = "Unable to reach the database. Showing demo vehicles.";
                    vehicleList = new List<Vehicle>();
                }

                if (vehicleList.Count == 0)
                {
                    _logger.LogWarning("Database returned zero vehicles. Loading sample fleet for display.");
                    var sample = VehicleSampleFactory.CreateSample();
                    vehicleList = sample.Vehicles;
                    primaryPhotos = new Dictionary<long, VehiclePhoto?>(sample.PrimaryPhotos);
                    ErrorMessage ??= "No vehicles found in the database. Showing demo fleet.";
                }

                var cards = new List<VehicleCardViewModel>(vehicleList.Count);
                VehicleCardViewModel? preferredSelection = null;

                _logger.LogInformation("Creating VehicleCardViewModel instances");
                foreach (var vehicle in vehicleList)
                {
                    primaryPhotos.TryGetValue(vehicle.Id, out var photo);
                    var card = new VehicleCardViewModel(vehicle, photo?.PhotoUrl, photo?.Caption, photo?.IsPrimary ?? false);
                    cards.Add(card);

                    if (preferredSelectionId.HasValue && vehicle.Id == preferredSelectionId.Value)
                    {
                        preferredSelection = card;
                    }
                }

            var typeOptions = cards
                .Select(c => c.CategoryDisplay)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            var transmissionOptions = cards
                .Select(c => c.TransmissionDisplay)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

                await RunOnUiThreadAsync(() =>
                {
                    _logger.LogInformation("Clearing Vehicles collection (current count: {CurrentCount})", Vehicles.Count);
                    Vehicles.Clear();

                    foreach (var card in cards)
                    {
                        Vehicles.Add(card);
                        _logger.LogDebug("Added vehicle card: {VehicleId} - {DisplayName}", card.Id, card.DisplayName);
                    }

                    _logger.LogInformation("Vehicles collection now contains {Count} items", Vehicles.Count);

                    UpdateLookupCollection(VehicleTypes, _allTypesOption, typeOptions, SelectedVehicleType, value => SelectedVehicleType = value);
                    UpdateLookupCollection(TransmissionOptions, _allTransmissionsOption, transmissionOptions, SelectedTransmission, value => SelectedTransmission = value);

                    ApplySortDescriptions();
                    _vehiclesView.Refresh();
                    SelectedVehicle = preferredSelection ?? Vehicles.FirstOrDefault();
                });

                System.Diagnostics.Debug.WriteLine($"=== LoadVehiclesAsync COMPLETED. Vehicle count: {Vehicles.Count}, Selected: {SelectedVehicle?.Id ?? 0} ===");
                _logger.LogInformation("LoadVehiclesAsync completed. Selected vehicle: {SelectedId}", SelectedVehicle?.Id ?? 0);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Vehicle loading was cancelled");
                // ignored
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load vehicles");
                ErrorMessage = "We couldn't load vehicles right now. Please try again.";
            }
            finally
            {
                IsLoading = false;
                _loadingCts?.Dispose();
                _loadingCts = null;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _localizationService.LanguageChanged -= OnLanguageChanged;
            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = null;
        }

        private void UpdateLookupCollection(ObservableCollection<string> target, string defaultOption, IEnumerable<string> values, string currentSelection, Action<string> setSelection)
        {
            var previousSelection = currentSelection;

            target.Clear();
            target.Add(defaultOption);

            foreach (var value in values)
            {
                target.Add(value);
            }

            var matchingSelection = target.FirstOrDefault(option =>
                string.Equals(option, previousSelection, StringComparison.OrdinalIgnoreCase));

            setSelection(matchingSelection ?? defaultOption);
        }

        private bool FilterVehicles(object obj)
        {
            if (obj is not VehicleCardViewModel vehicle)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim();
                var matchesSearch = vehicle.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || vehicle.PlateNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || vehicle.CategoryDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || vehicle.FuelDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || vehicle.TransmissionDisplay.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!matchesSearch)
                {
                    return false;
                }
            }

            if (!string.Equals(SelectedVehicleType, _allTypesOption, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(vehicle.CategoryDisplay, SelectedVehicleType, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.Equals(SelectedTransmission, _allTransmissionsOption, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(vehicle.TransmissionDisplay, SelectedTransmission, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (SelectedPriceRange is { IsDefault: false } range)
            {
                var rate = vehicle.DailyRate;
                if (rate < range.Min)
                {
                    return false;
                }

                if (range.Max.HasValue && rate > range.Max.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplySortDescriptions()
        {
            using (_vehiclesView.DeferRefresh())
            {
                _vehiclesView.SortDescriptions.Clear();
                if (SelectedSortOption != null)
                {
                    foreach (var sortDescription in SelectedSortOption.SortDescriptions)
                    {
                        _vehiclesView.SortDescriptions.Add(sortDescription);
                    }
                }
            }
        }

        private static Task RunOnUiThreadAsync(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                action();
                return Task.CompletedTask;
            }

            if (dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return dispatcher.InvokeAsync(action, DispatcherPriority.DataBind).Task;
        }
    }

    public class VehicleCardViewModel
    {
        public VehicleCardViewModel(Vehicle vehicle, string? primaryPhotoUrl, string? photoCaption, bool isPrimaryPhoto)
        {
            Entity = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
            PrimaryPhotoUrl = primaryPhotoUrl;
            PrimaryPhotoCaption = photoCaption;
            HasPrimaryPhoto = isPrimaryPhoto && !string.IsNullOrWhiteSpace(primaryPhotoUrl);
        }

        public Vehicle Entity { get; }

        public long Id => Entity.Id;

        public string DisplayName => string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Entity.ModelYear, Entity.Make, Entity.Model).Trim();

        public string Subtitle => string.Join(" | ", new[] { CategoryDisplay, TransmissionDisplay, FuelDisplay }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public string CategoryDisplay => Entity.Category.ToString();

        public string TransmissionDisplay => Entity.Transmission.ToString();

        public string FuelDisplay => Entity.Fuel.ToString();

        public byte Seats => Entity.Seats;

        public byte Doors => Entity.Doors;

        public string? Color => Entity.Color;

        public decimal DailyRate => Entity.DailyRate;

        public string DailyRateDisplay => Entity.DailyRate.ToString("C", CultureInfo.CurrentCulture);

        public string PlateNumber => Entity.PlateNumber;

        public VehicleStatus Status => Entity.Status;

        public DateTime CreatedAt => Entity.CreatedAt;

        public string? Description => Entity.Description;

        public string? PrimaryPhotoUrl { get; }

        public string? PrimaryPhotoCaption { get; }

        public bool HasPrimaryPhoto { get; }
    }

    public class PriceRangeOption
    {
        public PriceRangeOption(string displayName, decimal min, decimal? max, bool isDefault = false)
        {
            DisplayName = displayName;
            Min = min;
            Max = max;
            IsDefault = isDefault;
        }

        public string DisplayName { get; }

        public decimal Min { get; }

        public decimal? Max { get; }

        public bool IsDefault { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class VehicleSortOption
    {
        public VehicleSortOption(string displayName, params SortDescription[] sortDescriptions)
        {
            DisplayName = displayName;
            SortDescriptions = sortDescriptions ?? Array.Empty<SortDescription>();
        }

        public string DisplayName { get; }

        public IReadOnlyList<SortDescription> SortDescriptions { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}


