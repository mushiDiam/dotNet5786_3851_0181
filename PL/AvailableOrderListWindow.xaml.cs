using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BlApi;
using BO;

namespace PL
{
    public partial class AvailableOrderListWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;   // either managerId or courierId depending on mode
        private readonly int _managerId;
        private readonly bool _isManager;

        // simple unified view model used for the DataGrid regardless of user type
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

            // Subscribe to order observer to auto-refresh if BL supports it (best-effort).
            try
            {
                s_bl.Order.AddObserver(_requesterId, OrderObserver);
            }
            catch { }
        }

        private void AvailableOrderListWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_requesterId, OrderObserver); } catch { }
        }

        private void OrderObserver()
        {
            Dispatcher.Invoke(RefreshList);
        }

// Replaced RefreshList to add diagnostics and a fallback manual-filter check.
// This helps identify whether BL filtering (GetOpenOrder) removed items or there truly are no nearby orders.
        private void RefreshList()
        {
            try
            {
                if (_isManager)
                {
                    // Manager sees ALL orders (open). Use admin-level BL method.
                    var orders = s_bl.Order.GetOrders(_managerId, null, null, null)?.ToList()
                                 ?? new List<BO.OrderInList>();

                    var list = orders.Select(o => new OrderView
                    {
                        OrderId = o.OrderId,
                        AirDistance = o.AirDistance,
                        ScheduleStatus = o.ScheduleStatus,
                        RemainingTime = o.RemainingTime
                    }).ToList();

                    dgOrders.ItemsSource = list;

                    if (list.Count == 0)
                        MessageBox.Show("No orders found (manager view).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Courier: call BL method that should already filter by courier range
                    var openFromBl = s_bl.Order.GetOpenOrder(_managerId, _requesterId, null, null)?.ToList()
                                     ?? new List<BO.OpenOrderInList>();

                    // Map BL result (already filtered) to view
                    var blList = openFromBl.Select(o => new OrderView
                    {
                        OrderId = o.OrderId,
                        FullAddress = o.FullAddress ?? "",
                        AirDistance = o.AirDistance,
                        ScheduleStatus = o.ScheduleStatus,
                        RemainingTime = o.RemainingTime
                    }).ToList();

                    dgOrders.ItemsSource = blList;

                    // If BL returned nothing, gather diagnostic info to find out why
                    if (blList.Count == 0)
                    {
                        // 1) Try to read courier max distance
                        double courierMax = double.NaN;
                        try
                        {
                            var courier = s_bl.Courier.Details(_managerId, _requesterId);
                            courierMax = courier?.MaxDistancePreference ?? double.NaN;
                        }
                        catch { /* ignore */ }

                        // 2) Get all open orders via admin call and show which ones are within range according to simple air-distance filter
                        List<BO.OrderInList> allOpenAdmin = new();
                        try
                        {
                            allOpenAdmin = s_bl.Order.GetOrders(_managerId, null, null, null)?.ToList()
                                           ?? new List<BO.OrderInList>();
                        }
                        catch { /* ignore */ }

                        // 3) Compare using AirDistance from admin list (if available)
                        var withinByAir = allOpenAdmin
                            .Where(o => !double.IsNaN(courierMax) ? o.AirDistance <= courierMax : false)
                            .Select(o => new { o.OrderId, o.AirDistance})
                            .ToList();

                        string msg;
                        if (allOpenAdmin.Count == 0)
                        {
                            msg = "There are currently no open orders in the system.";
                        }
                        else if (!double.IsNaN(courierMax) && withinByAir.Count > 0)
                        {
                            // BL filtered out orders but simple air-distance test shows some within range -> BL filtering or async actual-distance check might be excluding them
                            var sample = string.Join("\n", withinByAir.Take(5).Select(x => $"#{x.OrderId} dist={x.AirDistance:F2}"));
                            msg = $"BL returned 0 available orders for courier.\nCourier MaxDistancePreference = {courierMax:F2} km\nTotal open orders: {allOpenAdmin.Count}\nOrders within range by AIR distance (sample):\n{sample}\n\nThis suggests BL's filtering may use a different metric (e.g. actual distance) or an authorization/parameter issue.";
                        }
                        else
                        {
                            // Nothing within range by air-distance
                            msg = $"BL returned 0 available orders for courier.\nCourier MaxDistancePreference = {(double.IsNaN(courierMax) ? "(unknown)" : courierMax.ToString("F2"))} km\nTotal open orders: {allOpenAdmin.Count}\nNo orders appear to be within courier's MaxDistancePreference by air-distance.";
                        }

                        MessageBox.Show(msg, "No Available Orders (diagnostic)", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ChooseSelectedOrder();

        private void ChooseSelectedOrder()
        {
            if (dgOrders.SelectedItem is not OrderView sel)
            {
                MessageBox.Show("Please select an order first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Caller of ChooseOrder must pass courierId as both first (authorization) and second parameter per BL
                if (_isManager)
                {
                    MessageBox.Show("Managers cannot choose an order on behalf of a courier. Sign in as a courier to accept an order.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int orderId = sel.OrderId;
                s_bl.Order.ChooseOrder(_requesterId, _requesterId, orderId);

                MessageBox.Show($"You chose order #{orderId}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh and close
                RefreshList();
                Close();
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
