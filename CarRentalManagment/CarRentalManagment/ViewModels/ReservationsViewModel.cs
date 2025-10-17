using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CarRentalManagment.Models;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class ReservationsViewModel : SectionViewModel
    {
        private readonly IUserSession _userSession;
        private readonly RelayCommand _createReservationCommand;
        private bool _isInitialized;

        public ReservationsViewModel(
            ReservationListViewModel reservationList,
            IUserSession userSession)
            : base("Reservations", "Create, manage, and review your upcoming rentals.")
        {
            ReservationList = reservationList;
            _userSession = userSession;

            _createReservationCommand = new RelayCommand(_ => OnCreateReservationRequested(), _ => CanCreateReservation);

            _userSession.CurrentUserChanged += OnCurrentUserChanged;
        }

        public ReservationListViewModel ReservationList { get; }

        public bool CanCreateReservation => _userSession.CurrentUser != null;

        public ICommand CreateReservationCommand => _createReservationCommand;

        public event EventHandler? CreateReservationRequested;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            await ReservationList.InitializeAsync();
        }

        public async Task RefreshAsync()
        {
            await ReservationList.LoadReservationsAsync();
        }

        public override void Dispose()
        {
            base.Dispose();
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;
            ReservationList.Dispose();
        }

        private void OnCreateReservationRequested()
        {
            CreateReservationRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCurrentUserChanged(object? sender, User? e)
        {
            _createReservationCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanCreateReservation));
        }
    }
}
