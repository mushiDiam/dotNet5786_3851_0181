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

            Loaded += Window_Loaded;
            Closed += Window_Closed;
        }

        // -----------------------------------------------------------------------
        // Filter & Sort Properties (Standard C# Properties)
        // -----------------------------------------------------------------------
        // Note: According to instructions, these are standard properties, NOT DependencyProperties.
        // Implication: Changing them in code does NOT automatically update the UI (must reset manually).

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

        public BO.CourierInList SelectedCourier
        {
            get { return (BO.CourierInList)GetValue(SelectedCourierProperty); }
            set { SetValue(SelectedCourierProperty, value); }
        }
        public static readonly DependencyProperty SelectedCourierProperty =
            DependencyProperty.Register(nameof(SelectedCourier), typeof(BO.CourierInList), typeof(CourierListWindow));

        // -----------------------------------------------------------------------
        // Event Handlers
        // -----------------------------------------------------------------------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            // Register Observer only if implemented in BL
            try { s_bl.Courier.AddObserver(CourierListObserver); } catch { }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            // Unregister Observer
            try { s_bl.Courier.RemoveObserver(CourierListObserver); } catch { }
        }

        private void CourierListObserver()
        {
            // UI updates must run on Dispatcher
            Dispatcher.Invoke(() => RefreshList());
        }

        private void RefreshList()
        {
            try
            {
                // 1. Fetch & Sort (via BL)
                var allCouriers = s_bl.Courier.GetCouriers(AdminId, null, SelectedSort);

                // 2. Filter (via PL)
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
            // 1. Reset logic properties
            TransportFilter = BO.Transportation.None;
            SelectedSort = null;

            // 2. Reset UI Controls manually (because properties are not DPs)
            cbFilter.SelectedValue = BO.Transportation.None;
            cbSort.SelectedIndex = -1; // Clear selection

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
                // Open for Update - using Show() to allow multi-window observation
                new CourierWindow(SelectedCourier.Id).Show();
            }
        }

        private void btnAddCourier_Click(object sender, RoutedEventArgs e)
        {
            // Open for Add
            new CourierWindow().Show();
        }

        private void btnDeleteCourier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BO.CourierInList courierToDelete)
            {
                var result = MessageBox.Show($"Delete {courierToDelete.FullName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        s_bl.Courier.Delete(AdminId, courierToDelete.Id);
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