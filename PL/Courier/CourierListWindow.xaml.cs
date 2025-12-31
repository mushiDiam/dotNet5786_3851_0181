using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BO;
using BlApi;
using System.Linq;

namespace PL.Courier
{
    public partial class CourierListWindow : Window
    {
        static readonly IBl s_bl = Factory.Get();

        public CourierListWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += CourierListWindow_Loaded;
        }

        private void CourierListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            try
            {
                // 1. Get Manager ID
                int adminId = s_bl.Admin.GetConfig().ManagerId;

                // 2. Call BL with the Sort parameter
                // We pass 'true' to include inactive couriers
                // We pass 'SelectedSort' (which is CourierInListOptions?) to sort the list
                var allCouriers = s_bl.Courier.GetCouriers(adminId, null, SelectedSort);

                // 3. Update the UI list
                CourierInList = allCouriers;
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

        #region SelectedSort (CHANGED)

        // Changed type from DeliveryTypes? to CourierInListOptions?
        public BO.CourierInListOptions? SelectedSort
        {
            get { return (BO.CourierInListOptions?)GetValue(SelectedSortProperty); }
            set { SetValue(SelectedSortProperty, value); }
        }

        public static readonly DependencyProperty SelectedSortProperty =
            DependencyProperty.Register(nameof(SelectedSort), typeof(BO.CourierInListOptions?), typeof(CourierListWindow),
                new PropertyMetadata(null, OnSortChanged)); // Trigger refresh when changed

        private static void OnSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CourierListWindow window)
            {
                window.RefreshList();
            }
        }

        #endregion

        // -----------------------------------------------------------------------
        // Event Handlers
        // -----------------------------------------------------------------------

        private void btnClearSort_Click(object sender, RoutedEventArgs e)
        {
            SelectedSort = null; // Reset sort
        }

        private void dgCourierList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Option A (Best Way): Use the bound property directly.
            // Since we have TwoWay binding on SelectedItem, 'SelectedCourier' is always updated.
            if (SelectedCourier != null)
            {
                // Open the window in Update mode (passing the ID)
                new CourierWindow(SelectedCourier.Id).ShowDialog();

                // Refresh list after window closes
                RefreshList();
            }
        }

        private void btnAddCourier_Click(object sender, RoutedEventArgs e)
        {
            // Open as Dialog to wait for close
            new CourierWindow().ShowDialog();
            RefreshList();
        }
        private void btnDeleteCourier_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the courier object from the button's data context
            // 'sender' is the button that was clicked
            if (sender is Button btn && btn.DataContext is BO.CourierInList courierToDelete)
            {
                // 2. Ask for confirmation
                var result = MessageBox.Show($"Are you sure you want to delete {courierToDelete.FullName}?",
                                             "Confirm Delete",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 3. Get Manager ID
                        int managerId = s_bl.Admin.GetConfig().ManagerId;

                        // 4. Call BL to delete
                        // Note: The signature in your interface is Delete(managerId, courierId)
                        s_bl.Courier.Delete(managerId, courierToDelete.Id);

                        // 5. Refresh the list to remove the deleted item from the screen
                        RefreshList();
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