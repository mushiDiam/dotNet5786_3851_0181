using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using BO;
using BlApi;

namespace PL.Courier
{
    public partial class CourierListWindow : Window
    {
        static readonly IBl s_bl = Factory.Get();
        static readonly int AdminId = s_bl.Admin.GetConfig().ManagerId;

        public CourierListWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Registration for events
            Loaded += Window_Loaded;
            Closed += Window_Closed;
        }

        // -----------------------------------------------------------------------
        // Event Handlers (Window Lifecycle)
        // -----------------------------------------------------------------------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Initial Data Load
            RefreshList();

            // 2. Register observer so BL will notify this window when the courier list changes
            try
            {
                s_bl.Courier.AddObserver(CourierListObserver);
            }
            catch
            {
                // Ignore observer registration errors
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            // Unregister observer to avoid memory leaks
            try
            {
                s_bl.Courier.RemoveObserver(CourierListObserver);
            }
            catch { }
        }

        // -----------------------------------------------------------------------
        // Main Logic (Observer & Refresh)
        // -----------------------------------------------------------------------

        // Observer method invoked by BL on list changes
        private void CourierListObserver()
        {
            // Ensure UI thread updates using Dispatcher
            Dispatcher.Invoke(() => RefreshList());
        }

        // ONE central method to handle fetching and filtering
        private void RefreshList()
        {
            try
            {
                // 1. Call BL with the Sort parameter (BL handles the sorting)
                // We pass 'true' to include inactive couriers
                var allCouriers = s_bl.Courier.GetCouriers(AdminId, true, SelectedSort);

                // 2. Apply UI-side filter by Transport (TransportFilter is bound from the ComboBox)
                var filtered = (TransportFilter == BO.Transportation.None) ?
                    allCouriers :
                    allCouriers.Where(c => c.Transport == TransportFilter);

                // 3. Update the UI list
                CourierInList = filtered;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error loading list: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------------
        // Dependency Properties
        // -----------------------------------------------------------------------

        #region CourierInList
        // This is the ONLY property used for the DataGrid ItemsSource
        public IEnumerable<BO.CourierInList> CourierInList
        {
            get { return (IEnumerable<BO.CourierInList>)GetValue(CourierInListProperty); }
            set { SetValue(CourierInListProperty, value); }
        }
        public static readonly DependencyProperty CourierInListProperty =
            DependencyProperty.Register(nameof(CourierInList), typeof(IEnumerable<BO.CourierInList>), typeof(CourierListWindow));
        #endregion

        #region SelectedCourier
        public BO.CourierInList SelectedCourier
        {
            get { return (BO.CourierInList)GetValue(SelectedCourierProperty); }
            set { SetValue(SelectedCourierProperty, value); }
        }
        public static readonly DependencyProperty SelectedCourierProperty =
            DependencyProperty.Register(nameof(SelectedCourier), typeof(BO.CourierInList), typeof(CourierListWindow));
        #endregion

        #region SelectedSort
        public BO.CourierInListOptions? SelectedSort
        {
            get { return (BO.CourierInListOptions?)GetValue(SelectedSortProperty); }
            set { SetValue(SelectedSortProperty, value); }
        }

        public static readonly DependencyProperty SelectedSortProperty =
            DependencyProperty.Register(nameof(SelectedSort), typeof(BO.CourierInListOptions?), typeof(CourierListWindow),
                new PropertyMetadata(null)); // We rely on SelectionChanged event to trigger refresh
        #endregion

        // -----------------------------------------------------------------------
        // Standard Properties
        // -----------------------------------------------------------------------

        // Bound via TwoWay to the UI. Since it's not a DependencyProperty, 
        // we rely on the SelectionChanged event to know when it changes.
        public BO.Transportation TransportFilter { get; set; } = BO.Transportation.None;

        // -----------------------------------------------------------------------
        // UI Interaction Handlers
        // -----------------------------------------------------------------------

        private void btnClearSort_Click(object sender, RoutedEventArgs e)
        {
            SelectedSort = BO.CourierInListOptions.None; // Reset sort
            TransportFilter = BO.Transportation.None;    // Reset filter

            // Note: Since TransportFilter is not a DP, the ComboBox UI might not update visually to "None" automatically
            // unless we implement INotifyPropertyChanged, but for now, we just refresh the list.
            RefreshList();
        }

        private void cbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshList();
        }

        private void cbTransport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshList();
        }

        private void dgCourierList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCourier != null)
            {
                // Open for Update (pass ID)
                new CourierWindow(SelectedCourier.Id).ShowDialog();
                // List refreshes automatically via Observer, but we can force it too
                // RefreshList(); 
            }
        }

        private void btnAddCourier_Click(object sender, RoutedEventArgs e)
        {
            // Open for Add (no ID)
            new CourierWindow().ShowDialog();
        }

        private void btnDeleteCourier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BO.CourierInList courierToDelete)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {courierToDelete.FullName}?",
                                             "Confirm Delete",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        s_bl.Courier.Delete(AdminId, courierToDelete.Id);
                        // No need to call RefreshList() manually here because the Observer will catch the change!
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}