using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarRentalManagment.Utilities;
using CarRentalManagment.ViewModels;

namespace CarRentalManagment.Views
{
    public partial class VehiclesView : UserControl
    {
        public VehiclesView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VehiclesViewModel viewModel)
            {
                await InitializeAsync(viewModel);
            }
        }

        private async Task InitializeAsync(VehiclesViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is VehiclesViewModel oldViewModel)
            {
                oldViewModel.AddVehicleRequested -= OnAddVehicleRequested;
                oldViewModel.ReserveRequested -= OnReserveRequested;
            }

            if (e.NewValue is VehiclesViewModel newViewModel)
            {
                newViewModel.AddVehicleRequested += OnAddVehicleRequested;
                newViewModel.ReserveRequested += OnReserveRequested;
            }
        }

        private async void OnAddVehicleRequested(object? sender, EventArgs e)
        {
            if (DataContext is not VehiclesViewModel viewModel)
            {
                return;
            }

            var dialog = CreateAddVehicleDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is AddVehicleViewModel addVehicleViewModel)
            {
                void OnDialogClosed(object? _, DialogCloseRequestedEventArgs args)
                {
                    dialog.DialogResult = args.DialogResult;
                    dialog.Close();
                }

                addVehicleViewModel.CloseRequested += OnDialogClosed;
                dialog.Closed += (_, _) => addVehicleViewModel.CloseRequested -= OnDialogClosed;
            }

            var result = dialog.ShowDialog();
            if (result == true)
            {
                await viewModel.ReloadVehiclesAsync();
            }
        }

        private async void OnReserveRequested(object? sender, VehicleCardViewModel? e)
        {
            if (DataContext is not VehiclesViewModel viewModel || e == null)
            {
                return;
            }

            var dialog = CreateReservationDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.DataContext is CreateReservationViewModel reservationViewModel)
            {
                await reservationViewModel.InitializeAsync(e);

                void OnDialogClosed(object? _, DialogCloseRequestedEventArgs args)
                {
                    dialog.DialogResult = args.DialogResult;
                    dialog.Close();
                }

                reservationViewModel.CloseRequested += OnDialogClosed;
                dialog.Closed += (_, _) => reservationViewModel.CloseRequested -= OnDialogClosed;
            }

            var result = dialog.ShowDialog();
            if (result == true)
            {
                MessageBox.Show($"Reservation created for {e.DisplayName}.", "Reservation Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private AddVehicleDialog CreateAddVehicleDialog()
        {
            var dialog = new AddVehicleDialog
            {
                DataContext = AppServices.GetRequiredService<AddVehicleViewModel>()
            };

            return dialog;
        }

        private CreateReservationDialog CreateReservationDialog()
        {
            return new CreateReservationDialog
            {
                DataContext = AppServices.GetRequiredService<CreateReservationViewModel>()
            };
        }
    }
}
