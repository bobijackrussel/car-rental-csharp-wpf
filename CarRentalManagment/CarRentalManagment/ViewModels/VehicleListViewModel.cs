using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.ViewModels
{
    public class VehicleListViewModel : BaseViewModel
    {
        private readonly IVehicleService _vehicleService;
        private readonly IVehiclePhotoService _vehiclePhotoService;
        private readonly ILogger<VehicleListViewModel> _logger;

        private readonly ICollectionView _vehiclesView;
        private readonly RelayCommand _refreshCommand;
        private readonly RelayCommand _clearSearchCommand;

        private VehicleCardViewModel? _selectedVehicle;
        private string _searchText = string.Empty;
        private VehicleSortOption? _selectedSortOption;
        private bool _isLoading;
        private string? _errorMessage;
        private CancellationTokenSource? _loadingCts;

        public VehicleListViewModel(
            IVehicleService vehicleService,
            IVehiclePhotoService vehiclePhotoService,
            ILogger<VehicleListViewModel> logger)
        {
            _vehicleService = vehicleService;
            _vehiclePhotoService = vehiclePhotoService;
            _logger = logger;

            Vehicles = new ObservableCollection<VehicleCardViewModel>();
            _vehiclesView = CollectionViewSource.GetDefaultView(Vehicles);
            _vehiclesView.Filter = FilterVehicles;

            SortOptions = new ObservableCollection<VehicleSortOption>
            {
                new("Recommended", new SortDescription(nameof(VehicleCardViewModel.DisplayName), ListSortDirection.Ascending)),
                new("Daily Rate (Low to High)", new SortDescription(nameof(VehicleCardViewModel.DailyRate), ListSortDirection.Ascending)),
                new("Daily Rate (High to Low)", new SortDescription(nameof(VehicleCardViewModel.DailyRate), ListSortDirection.Descending)),
                new("Newest Added", new SortDescription(nameof(VehicleCardViewModel.CreatedAt), ListSortDirection.Descending))
            };

            _selectedSortOption = SortOptions.FirstOrDefault();

            _refreshCommand = new RelayCommand(async _ => await LoadVehiclesAsync(), _ => !IsLoading);
            _clearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => !IsLoading && !string.IsNullOrWhiteSpace(SearchText));
        }

        public ObservableCollection<VehicleCardViewModel> Vehicles { get; }

        public ICollectionView VehiclesView => _vehiclesView;

        public ObservableCollection<VehicleSortOption> SortOptions { get; }

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

        public VehicleCardViewModel? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (SetProperty(ref _selectedVehicle, value))
                {
                    SelectedVehicleChanged?.Invoke(this, value);
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

        public event EventHandler<VehicleCardViewModel?>? SelectedVehicleChanged;

        public async Task LoadVehiclesAsync(long? preferredSelectionId = null, CancellationToken cancellationToken = default)
        {
            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var token = _loadingCts.Token;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var vehicles = await _vehicleService.GetAllAsync(token);
                var ids = vehicles.Select(v => v.Id).Where(id => id > 0).Distinct().ToList();

                IDictionary<long, VehiclePhoto?>? primaryPhotos = null;
                if (ids.Count > 0)
                {
                    primaryPhotos = await _vehiclePhotoService.GetPrimaryPhotosAsync(ids, token);
                }

                Vehicles.Clear();
                VehicleCardViewModel? preferredSelection = null;

                foreach (var vehicle in vehicles)
                {
                    primaryPhotos?.TryGetValue(vehicle.Id, out var photo);
                    var card = new VehicleCardViewModel(vehicle, photo?.PhotoUrl, photo?.Caption, photo?.IsPrimary ?? false);
                    Vehicles.Add(card);

                    if (preferredSelectionId.HasValue && vehicle.Id == preferredSelectionId.Value)
                    {
                        preferredSelection = card;
                    }
                }

                ApplySortDescriptions();
                _vehiclesView.Refresh();

                SelectedVehicle = preferredSelection ?? Vehicles.FirstOrDefault();
            }
            catch (OperationCanceledException)
            {
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
            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = null;
        }

        private bool FilterVehicles(object obj)
        {
            if (obj is not VehicleCardViewModel vehicle)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var search = SearchText.Trim();
            return vehicle.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || vehicle.PlateNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || vehicle.CategoryDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || vehicle.FuelDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || vehicle.TransmissionDisplay.Contains(search, StringComparison.OrdinalIgnoreCase);
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

        public string Subtitle => string.Join(" • ", new[] { CategoryDisplay, TransmissionDisplay, FuelDisplay }.Where(s => !string.IsNullOrWhiteSpace(s)));

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
