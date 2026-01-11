using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BlApi;
using BO;
using PL;

namespace PL
{
    public partial class AvailableOrderListWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly int _managerId;
        private readonly bool _isManager;

        // Property for Binding to the View
        public Visibility CourierVisibility { get; set; }

        private sealed class OrderView
        {
            public int OrderId { get; init; }
            public string CustomerName { get; init; } = "";
            public string FullAddress { get; init; } = "";
            public double AirDistance { get; init; }
            public BO.ScheduleStatus? ScheduleStatus { get; init; }
            public TimeSpan? RemainingTime { get; init; }
        }

        public AvailableOrderListWindow(int requesterId, bool isManager = false)
        {
            // Determine visibility before InitializeComponent
            CourierVisibility = isManager ? Visibility.Collapsed : Visibility.Visible;

            InitializeComponent();
            DataContext = this;
            _requesterId = requesterId;
            _managerId = s_bl.Admin.GetConfig().ManagerId;
            _isManager = isManager;

            Loaded += AvailableOrderListWindow_Loaded;
            Closed += AvailableOrderListWindow_Closed;
        }

        private void AvailableOrderListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            try { s_bl.Order.AddObserver(_requesterId, OrderObserver); } catch { }
        }

        private void AvailableOrderListWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_requesterId, OrderObserver); } catch { }
        }

        private void OrderObserver()
        {
            Dispatcher.Invoke(RefreshList);
        }

        private void RefreshList()
        {
            try
            {
                if (_isManager)
                {
                    var orders = s_bl.Order.GetOrders(_managerId, null, null, null)?.ToList() ?? new List<BO.OrderInList>();
                    var list = orders.Select(o => new OrderView { OrderId = o.OrderId, AirDistance = o.AirDistance, ScheduleStatus = o.ScheduleStatus, RemainingTime = o.RemainingTime }).ToList();
                    dgOrders.ItemsSource = list;
                }
                else
                {
                    var openFromBl = s_bl.Order.GetOpenOrder(_managerId, _requesterId, null, null)?.ToList() ?? new List<BO.OpenOrderInList>();
                    var blList = openFromBl.Select(o => new OrderView { OrderId = o.OrderId, FullAddress = o.FullAddress ?? "", AirDistance = o.AirDistance, ScheduleStatus = o.ScheduleStatus, RemainingTime = o.RemainingTime }).ToList();
                    dgOrders.ItemsSource = blList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed loading available orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                dgOrders.ItemsSource = new List<OrderView>();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshList();
        private void BtnChoose_Click(object sender, RoutedEventArgs e) => ChooseSelectedOrder();

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOrders.SelectedItem is not OrderView sel) return;

            try
            {
                var detailsWindow = new PL.AvailableOrders.OrderDetailsWindow(_requesterId, sel.OrderId, _isManager) { Owner = this };
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open order details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChooseSelectedOrder()
        {
            if (dgOrders.SelectedItem is not OrderView sel)
            {
                MessageBox.Show("Please select an order first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                if (_isManager)
                {
                    // Double check in logic, though button is hidden
                    MessageBox.Show("Managers cannot choose an order on behalf of a courier.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                int orderId = sel.OrderId;
                s_bl.Order.ChooseOrder(_requesterId, _requesterId, orderId);
                MessageBox.Show($"You chose order #{orderId}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to choose order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_requesterId, OrderObserver); } catch { }
            Close();
        }
    }
}