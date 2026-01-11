using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BlApi;
using BO;
using PL.AvailableOrders;

namespace PL.Deliveries
{
    /// <summary>
    /// Interaction logic for DeliveriesListWindow.xaml
    /// </summary>
    public partial class DeliveriesListWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly bool _isManager;
        private readonly int _managerIdConfig;

        // Lightweight view model for the grid
        public class DeliveryView
        {
            public int DeliveryId { get; set; }           // <-- added
            public int OrderId { get; set; }
            public string CustomerName { get; set; }
            public string CourierName { get; set; }
            // Use string so the UI shows exactly the delivery end status (or order status fallback)
            public string Status { get; set; }
            public DateTime? PickedUp { get; set; }
            public DateTime? Delivered { get; set; }
            public string Address { get; set; }
        }

        public DeliveriesListWindow(int requesterId, bool isManager)
        {
            InitializeComponent();
            _requesterId = requesterId;
            _isManager = isManager;
            _managerIdConfig = s_bl.Admin.GetConfig().ManagerId;

            RefreshList();
        }

        private void RefreshList()
        {
            try
            {
                // 1. Get the list of lightweight orders (OrderInList)
                // GetOrders returns IEnumerable<BO.OrderInList>
                var rawList = s_bl.Order.GetOrders(_managerIdConfig, null, null, null);

                // 2. Normalize to a list of full BO.Order objects.
                var allOrders = new List<BO.Order>();

                foreach (var item in rawList)
                {
                    // Extract OrderId from OrderInList and fetch full order details
                    int id = item.OrderId;

                    BO.Order fullOrder = s_bl.Order.Details(_managerIdConfig, id);

                    if (fullOrder != null)
                    {
                        allOrders.Add(fullOrder);
                    }
                }

                // 3. Filter logic
                IEnumerable<BO.Order> filteredOrders;
                if (_isManager)
                {
                    // Manager sees orders that have any history in the 'Deliveries' list
                    filteredOrders = allOrders.Where(o => o.Deliveries != null && o.Deliveries.Any());
                }
                else
                {
                    // Courier sees only orders where they specifically appear in the delivery history
                    filteredOrders = allOrders.Where(o =>
                        o.Deliveries != null &&
                        o.Deliveries.Any(d => d.CourierId == _requesterId));
                }

                // 4. Convert to View Model for DataGrid
                var gridData = filteredOrders.Select(o =>
                {
                    // Get the relevant delivery info: the delivery matching the courier (if not manager) or most recent one.
                    BO.DeliveryPerOrderInList? relevantDelivery = null;

                    if (_isManager)
                    {
                        // manager: show the most recent delivery for the order
                        relevantDelivery = o.Deliveries?.LastOrDefault();
                    }
                    else
                    {
                        // courier: show the most recent delivery performed by this courier for the order
                        relevantDelivery = o.Deliveries?.Where(d => d.CourierId == _requesterId).LastOrDefault()
                                         ?? o.Deliveries?.LastOrDefault();
                    }

                    // Determine display status: prefer delivery-level status when available,
                    // otherwise fallback to order-level status.
                    string statusText = relevantDelivery?.OrderStatus?.ToString() ?? o.OrderStatus.ToString();

                    return new DeliveryView
                    {
                        DeliveryId = relevantDelivery?.DeliveryId ?? 0, // <-- set DeliveryId
                        OrderId = o.Id,
                        CustomerName = o.CustomerName,
                        CourierName = relevantDelivery?.CourierName ?? "N/A",
                        Status = statusText,
                        PickedUp = relevantDelivery?.StartTime,
                        Delivered = relevantDelivery?.EndTime,
                        Address = o.FullAddress ?? ""
                    };
                }).OrderByDescending(x => x.OrderId).ToList();

                dgDeliveries.ItemsSource = gridData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load deliveries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Opens OrderDetailsWindow when a delivery row is double-clicked.
        // Ensure the window shows the actual end-status of the delivery that was double-clicked.
        private void dgDeliveries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDeliveries.SelectedItem is not DeliveryView dv) return;

            try
            {
                // Fetch full order details from BL
                var order = s_bl.Order.Details(_managerIdConfig, dv.OrderId);
                if (order == null)
                {
                    MessageBox.Show("Order details not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // If the user double-clicked an ended delivery, prefer showing the order with
                // the OrderStatus overridden by the delivery's end status so the details window
                // reflects how that delivery ended.
                if (dv.DeliveryId != 0 && order.Deliveries != null)
                {
                    var matching = order.Deliveries.FirstOrDefault(d => d.DeliveryId == dv.DeliveryId);
                    if (matching != null && matching.OrderStatus.HasValue)
                    {
                        // Temporarily set the order's OrderStatus so the existing details view (which binds to Order.OrderStatus)
                        // displays the delivery-level end status instead of a generic order status computed elsewhere.
                        order.OrderStatus = matching.OrderStatus.Value;
                    }
                }

                // Use existing BO.Order constructor (read-only view)
                var wnd = new OrderDetailsWindow(order)
                {
                    Owner = this
                };
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open order details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}