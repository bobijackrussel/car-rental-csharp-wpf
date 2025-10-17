using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CarRentalManagment.Services;
using CarRentalManagment.Utilities.Commands;

namespace CarRentalManagment.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IUserSession _userSession;
        private readonly Func<VehiclesViewModel> _vehiclesFactory;
        private readonly Func<ReservationsViewModel> _reservationsFactory;
        private readonly Func<FeedbackViewModel> _feedbackFactory;
        private readonly Func<ReportViolationViewModel> _reportFactory;
        private readonly Func<SettingsViewModel> _settingsFactory;

        private BaseViewModel? _currentSection;
        private bool _isMenuOpen;

        public MainViewModel(
            INavigationService navigationService,
            IUserSession userSession,
            Func<VehiclesViewModel> vehiclesFactory,
            Func<ReservationsViewModel> reservationsFactory,
            Func<FeedbackViewModel> feedbackFactory,
            Func<ReportViolationViewModel> reportFactory,
            Func<SettingsViewModel> settingsFactory)
        {
            _navigationService = navigationService;
            _userSession = userSession;
            _vehiclesFactory = vehiclesFactory;
            _reservationsFactory = reservationsFactory;
            _feedbackFactory = feedbackFactory;
            _reportFactory = reportFactory;
            _settingsFactory = settingsFactory;

            _userSession.CurrentUserChanged += OnCurrentUserChanged;

            ToggleMenuCommand = new RelayCommand(_ => ToggleMenu());
            LogoutCommand = new RelayCommand(_ => Logout());

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new("Vehicles", "🚗", new RelayCommand(_ => NavigateToSection(_vehiclesFactory()))),
                new("Reservations", "🗓", new RelayCommand(_ => NavigateToSection(_reservationsFactory()))),
                new("Leave Feedback", "💬", new RelayCommand(_ => NavigateToSection(_feedbackFactory()))),
                new("Report Violation", "⚠️", new RelayCommand(_ => NavigateToSection(_reportFactory()))),
                new("Settings", "⚙️", new RelayCommand(_ => NavigateToSection(_settingsFactory())))
            };

            NavigateToSection(_vehiclesFactory());
        }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; }

        public BaseViewModel? CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        public ICommand ToggleMenuCommand { get; }
        public ICommand LogoutCommand { get; }

        public string WelcomeMessage
        {
            get
            {
                var user = _userSession.CurrentUser;
                return user == null
                    ? "Welcome"
                    : $"Welcome, {user.FirstName} {user.LastName}".Trim();
            }
        }

        public override void Dispose()
        {
            _userSession.CurrentUserChanged -= OnCurrentUserChanged;

            if (CurrentSection is { })
            {
                CurrentSection.Dispose();
            }

            base.Dispose();
        }

        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private void Logout()
        {
            _userSession.CurrentUser = null;
            IsMenuOpen = false;
            _navigationService.NavigateTo<LoginSignupViewModel>();
        }

        private void NavigateToSection(BaseViewModel viewModel)
        {
            CurrentSection?.Dispose();
            CurrentSection = viewModel;
            IsMenuOpen = false;
        }

        private void OnCurrentUserChanged(object? sender, Models.User? e)
        {
            OnPropertyChanged(nameof(WelcomeMessage));
        }

        public record MenuItemViewModel(string Title, string Icon, ICommand Command);
    }
}
