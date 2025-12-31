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
            if (SelectedCourier != null)
            {
                // Open as Dialog to wait for close
                new CourierWindow(SelectedCourier.Id).ShowDialog();
                RefreshList();
            }
        }

        private void btnAddCourier_Click(object sender, RoutedEventArgs e)
        {
            // Open as Dialog to wait for close
            new CourierWindow().ShowDialog();
            RefreshList();
        }
    }
}