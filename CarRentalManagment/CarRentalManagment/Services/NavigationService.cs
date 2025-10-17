using System;
using System.Collections.Concurrent;
using CarRentalManagment.ViewModels;
using Microsoft.Extensions.Logging;

namespace CarRentalManagment.Services
{
    public class NavigationService : INavigationService
    {
        private readonly ConcurrentDictionary<Type, Func<BaseViewModel>> _factories = new();
        private readonly ILogger<NavigationService> _logger;
        private BaseViewModel? _currentViewModel;

        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger;
        }

        public BaseViewModel? CurrentViewModel => _currentViewModel;

        public event EventHandler<BaseViewModel?>? CurrentViewModelChanged;

        public void Register<TViewModel>(Func<TViewModel> factory) where TViewModel : BaseViewModel
        {
            var type = typeof(TViewModel);
            _factories[type] = () => factory();
        }

        public bool NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var type = typeof(TViewModel);
            if (!_factories.TryGetValue(type, out var factory))
            {
                _logger.LogWarning("No view model registered for type {ViewModelType}", type.Name);
                return false;
            }

            var viewModel = factory();
            UpdateCurrentViewModel(viewModel);
            return true;
        }

        private void UpdateCurrentViewModel(BaseViewModel viewModel)
        {
            var previousViewModel = _currentViewModel;
            if (ReferenceEquals(previousViewModel, viewModel))
            {
                return;
            }

            previousViewModel?.Dispose();
            _currentViewModel = viewModel;
            CurrentViewModelChanged?.Invoke(this, _currentViewModel);
        }
    }
}
