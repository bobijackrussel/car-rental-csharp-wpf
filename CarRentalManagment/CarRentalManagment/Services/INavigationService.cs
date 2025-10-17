using System;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Services
{
    public interface INavigationService
    {
        BaseViewModel? CurrentViewModel { get; }
        event EventHandler<BaseViewModel?>? CurrentViewModelChanged;
        void Register<TViewModel>(Func<TViewModel> factory) where TViewModel : BaseViewModel;
        bool NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    }
}
