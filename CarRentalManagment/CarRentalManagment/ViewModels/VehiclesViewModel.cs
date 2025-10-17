using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class VehiclesViewModel : SectionViewModel
    {
        private readonly IUserSession _userSession;
        private readonly RelayCommand _addVehicleCommand;
        private bool _isInitialized;

        public VehiclesViewModel(
            VehicleListViewModel vehicleList,
            VehicleDetailsViewModel vehicleDetails,
            IUserSession userSession)
            : base("Vehicles", "Browse and manage the vehicles available for rent.")
        {
            VehicleList = vehicleList;
            VehicleDetails = vehicleDetails;
            _userSession = userSession;

            VehicleList.SelectedVehicleChanged += OnSelectedVehicleChanged;
            VehicleDetails.ReserveRequested += OnReserveRequested;
            _userSession.CurrentUserChanged += OnCurrentUserChanged;

            _addVehicleCommand = new RelayCommand(_ => OnAddVehicleRequested(), _ => CanAddVehicle);
        }

        public VehicleListViewModel VehicleList { get; }

        public VehicleDetailsViewModel VehicleDetails { get; }

        public bool CanAddVehicle => _userSession.CurrentUser != null;

        public ICommand AddVehicleCommand => _addVehicleCommand;

        public event EventHandler? AddVehicleRequested;

        public event EventHandler<VehicleCardViewModel?>? ReserveRequested;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            await VehicleList.LoadVehiclesAsync();
            await VehicleDetails.SetVehicleAsync(VehicleList.SelectedVehicle);
        }

        public async Task ReloadVehiclesAsync()
        {
            var previousId = VehicleList.SelectedVehicle?.Id;
            await VehicleList.LoadVehiclesAsync(previousId);
            await VehicleDetails.SetVehicleAsync(VehicleList.SelectedVehicle);
        }

        public override void Dispose()
        {
            base.Dispose();
            VehicleList.SelectedVehicleChanged -= OnSelectedVehicleChanged;
            VehicleDetails.ReserveRequested -= OnReserveRequested;
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            VehicleList.Dispose();
            VehicleDetails.Dispose();
        }

        private void OnAddVehicleRequested()
        {
            AddVehicleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectedVehicleChanged(object? sender, VehicleCardViewModel? e)
        {
            _ = VehicleDetails.SetVehicleAsync(e);
        }

        private void OnReserveRequested(object? sender, VehicleCardViewModel? e)
        {
            ReserveRequested?.Invoke(this, e);
        }

        private void OnCurrentUserChanged(object? sender, Models.User? e)
        {
            OnPropertyChanged(nameof(CanAddVehicle));
            _addVehicleCommand.RaiseCanExecuteChanged();
        }
    }
}
