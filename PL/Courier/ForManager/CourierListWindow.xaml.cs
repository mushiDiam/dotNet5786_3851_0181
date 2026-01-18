using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using BO;
using BlApi;
using PL.Helpers;

namespace PL.Courier.ForManager
{
    public partial class CourierListWindow : Window
    {
        static readonly IBl s_bl = Factory.Get();
        static readonly int AdminId = s_bl.Admin.GetConfig().ManagerId;

        // Stage 7: Add mutex
        private readonly ObserverMutex _courierListMutex = new();


        public CourierListWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += Window_Loaded;
            Closed += Window_Closed;
        }

        // -----------------------------------------------------------------------
        // Filter & Sort Properties (Standard C# Properties)
        // -----------------------------------------------------------------------
        public BO.Transportation TransportFilter { get; set; } = BO.Transportation.None;
        public BO.CourierInListOptions? SelectedSort { get; set; } = null;

        // -----------------------------------------------------------------------
        // Dependency Properties (Only for List and Selection)
        // -----------------------------------------------------------------------
        public IEnumerable<BO.CourierInList> CourierInList
        {
            get { return (IEnumerable<BO.CourierInList>)GetValue(CourierInListProperty); }
            set { SetValue(CourierInListProperty, value); }
        }
        public static readonly DependencyProperty CourierInListProperty =
            DependencyProperty.Register(nameof(CourierInList), typeof(IEnumerable<BO.CourierInList>), typeof(CourierListWindow));

        public BO.CourierInList? SelectedCourier { get; set; }

        // -----------------------------------------------------------------------
        // Event Handlers
        // -----------------------------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            try { s_bl.Courier.AddObserver(CourierListObserver); } catch { }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Courier.RemoveObserver(CourierListObserver); } catch { }
        }

        // Stage 7: Updated observer
        private void CourierListObserver()
        {
            // Check and prevent double entry
            if (_courierListMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;        // Queue work on UI thread
            Dispatcher.BeginInvoke(async () =>
            {
                // The actual work
                RefreshList();            // Check if restart needed
                if (await _courierListMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    CourierListObserver();
            });
        }

        private void RefreshList()
        {
            try
            {
                // Ask BL for sorted list (SelectedSort passed to BL)
                var allCouriers = s_bl.Courier.GetCouriers(AdminId, null, SelectedSort);

                // Apply transport filter in PL
                var filtered = (TransportFilter == BO.Transportation.None) ?
                    allCouriers :
                    allCouriers.Where(c => c.Transport == TransportFilter);

                CourierInList = filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading list: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------------
        // UI Interaction
        // -----------------------------------------------------------------------
        private void btnClearSort_Click(object sender, RoutedEventArgs e)
        {
            // Reset logic properties
            TransportFilter = BO.Transportation.None;
            SelectedSort = null;

            // Reset UI Controls manually (because properties are not DPs)
            cbFilter.SelectedValue = BO.Transportation.None;
            cbSort.SelectedIndex = -1;

            RefreshList();
        }

        // Ensure selection handlers update the logic properties before refreshing
        private void cbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSort.SelectedItem is BO.CourierInListOptions sort)
                SelectedSort = sort;
            else
                SelectedSort = null;

            RefreshList();
        }

        private void cbTransport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFilter.SelectedItem is BO.Transportation t)
                TransportFilter = t;
            else
                TransportFilter = BO.Transportation.None;

            RefreshList();
        }

        private void dgCourierList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCourier != null)
            {
                CourierWindow courierWindow = new CourierWindow(SelectedCourier.Id);
                courierWindow.Show();
            }
        }

        private void btnAddCourier_Click(object sender, RoutedEventArgs e)
        {
            CourierWindow courierWindow = new CourierWindow();
            courierWindow.Show();
        }

        // IMPORTANT: guard deletion and refresh afterwards
        private void btnDeleteCourier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BO.CourierInList courierToDelete)
            {
                // Check deletability: no completed deliveries and no active order
                // We assume OrdersOnTime + OrdersLate == 0 and CurrentOrderId == null mean deletable
                bool hasPastDeliveries = (courierToDelete.OrdersOnTime + courierToDelete.OrdersLate) > 0;
                bool hasActiveOrder = courierToDelete.CurrentOrderId.HasValue;

                if (hasPastDeliveries || hasActiveOrder)
                {
                    MessageBox.Show("Courier cannot be deleted because they have deliveries or an active order.", "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"Delete {courierToDelete.FullName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        s_bl.Courier.Delete(AdminId, courierToDelete.Id);
                        RefreshList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed: {ex.Message}");
                    }
                }
            }
        }
    }
}