using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlApi;
using BO;
using PL;
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
            public int DeliveryId { get; set; }
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

            // Populate both ComboBoxes with the same enum values (fields usable for sorting/filtering)
            cmbSort.ItemsSource = Enum.GetValues(typeof(OrderInListOptions)).Cast<OrderInListOptions>();
            cmbFilter.ItemsSource = Enum.GetValues(typeof(OrderInListOptions)).Cast<OrderInListOptions>();

            // Optionally set default selection
            cmbSort.SelectedItem = OrderInListOptions.OrderId;

            RefreshList();
        }

        private void RefreshList(OrderInListOptions? sortBy = null, OrderInListOptions? filterBy = null)
        {
            try
            {
                // 1. Get the list of lightweight orders (OrderInList)
                var rawList = s_bl.Order.GetOrders(_managerIdConfig, null, null, null);

                // 2. Normalize to a list of full BO.Order objects.
                var allOrders = new List<BO.Order>();

                foreach (var item in rawList)
                {
                    int id = item.OrderId;
                    BO.Order fullOrder = s_bl.Order.Details(_managerIdConfig, id);
                    if (fullOrder != null)
                        allOrders.Add(fullOrder);
                }

                // 3. Filter logic by requester (manager or courier)
                IEnumerable<BO.Order> filteredOrders;
                if (_isManager)
                {
                    filteredOrders = allOrders.Where(o => o.Deliveries != null && o.Deliveries.Any());
                }
                else
                {
                    filteredOrders = allOrders.Where(o =>
                        o.Deliveries != null &&
                        o.Deliveries.Any(d => d.CourierId == _requesterId));
                }

                // Refresh the summary counts UI based on the filtered orders
                RefreshSummary(filteredOrders);

                // 4. Convert to View Model for DataGrid
                var gridQuery = filteredOrders.Select(o =>
                {
                    BO.DeliveryPerOrderInList? relevantDelivery = null;

                    if (_isManager)
                    {
                        relevantDelivery = o.Deliveries?.LastOrDefault();
                    }
                    else
                    {
                        relevantDelivery = o.Deliveries?.Where(d => d.CourierId == _requesterId).LastOrDefault()
                                         ?? o.Deliveries?.LastOrDefault();
                    }

                    string statusText = relevantDelivery?.OrderStatus?.ToString() ?? o.OrderStatus.ToString();

                    return new DeliveryView
                    {
                        DeliveryId = relevantDelivery?.DeliveryId ?? 0,
                        OrderId = o.Id,
                        CustomerName = o.CustomerName,
                        CourierName = relevantDelivery?.CourierName ?? "N/A",
                        Status = statusText,
                        PickedUp = relevantDelivery?.StartTime,
                        Delivered = relevantDelivery?.EndTime,
                        Address = o.FullAddress ?? ""
                    };
                });

                // 5. Apply filtering based on selected enum field. Both combo boxes use the same enum list.
                if (filterBy.HasValue)
                {
                    switch (filterBy.Value)
                    {
                        case OrderInListOptions.DeliveryId:
                            gridQuery = gridQuery.Where(x => x.DeliveryId != 0);
                            break;
                        case OrderInListOptions.OrderId:
                            // no-op: all rows have OrderId
                            break;
                        case OrderInListOptions.OrderStatus:
                            gridQuery = gridQuery.Where(x => !string.IsNullOrWhiteSpace(x.Status));
                            break;
                        case OrderInListOptions.CompletionTime:
                            gridQuery = gridQuery.Where(x => x.Delivered.HasValue);
                            break;
                        default:
                            break;
                    }
                }

                // 6. Apply sorting based on selected enum field.
                if (sortBy.HasValue)
                {
                    switch (sortBy.Value)
                    {
                        case OrderInListOptions.DeliveryId:
                            gridQuery = gridQuery.OrderByDescending(x => x.DeliveryId);
                            break;
                        case OrderInListOptions.OrderId:
                            gridQuery = gridQuery.OrderByDescending(x => x.OrderId);
                            break;
                        case OrderInListOptions.OrderStatus:
                            gridQuery = gridQuery.OrderBy(x => x.Status);
                            break;
                        case OrderInListOptions.CompletionTime:
                            gridQuery = gridQuery.OrderBy(x => x.Delivered);
                            break;
                        default:
                            gridQuery = gridQuery.OrderByDescending(x => x.OrderId);
                            break;
                    }
                }
                else
                {
                    gridQuery = gridQuery.OrderByDescending(x => x.OrderId);
                }

                var gridData = gridQuery.ToList();
                dgDeliveries.ItemsSource = gridData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load deliveries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Build and populate the summary counts of orders by OrderStatus x ScheduleStatus.
        // Clicking a count opens the Order list window pre-filtered for that combination.
        private void RefreshSummary(IEnumerable<BO.Order> filteredOrders)
        {
            wpSummary.Children.Clear();

            var orderStatuses = Enum.GetValues(typeof(BO.OrderStatus)).Cast<BO.OrderStatus>();
            var scheduleStatuses = Enum.GetValues(typeof(BO.ScheduleStatus)).Cast<BO.ScheduleStatus>();

            foreach (var os in orderStatuses)
            {
                foreach (var ss in scheduleStatuses)
                {
                    int count = filteredOrders.Count(o => o.OrderStatus == os && o.ScheduleStatus == ss);

                    // show only combos with any items to reduce clutter
                    if (count == 0) continue;

                    var btn = new Button
                    {
                        Content = $"{os} / {ss}: {count}",
                        Margin = new Thickness(4),
                        Padding = new Thickness(8, 4, 8, 4),
                        MinWidth = 180,
                        Tag = Tuple.Create(os, ss),
                        Style = (Style)FindResource("ModernButton")
                    };
                    btn.Click += SummaryButton_Click;
                    wpSummary.Children.Add(btn);
                }
            }
        }

        private void SummaryButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Tuple<BO.OrderStatus, BO.ScheduleStatus> tag)
            {
                try
                {
                    // Open the order list window pre-filtered for the selected status combination.
                    var wnd = new AvailableOrderListWindow(_managerIdConfig, true, tag.Item1, tag.Item2)
                    {
                        Owner = this
                    };
                    wnd.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open filtered order list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Helper to safely read selected enum value from ComboBox.
        private static OrderInListOptions? GetSelectedOrderInListOption(ComboBox cb)
        {
            return cb?.SelectedItem is OrderInListOptions val ? val : (OrderInListOptions?)null;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var sortSel = GetSelectedOrderInListOption(cmbSort);
            var filterSel = GetSelectedOrderInListOption(cmbFilter);
            RefreshList(sortSel, filterSel);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SortCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var sortSel = GetSelectedOrderInListOption(cmbSort);
            var filterSel = GetSelectedOrderInListOption(cmbFilter);
            RefreshList(sortSel, filterSel);
        }

        private void FilterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var sortSel = GetSelectedOrderInListOption(cmbSort);
            var filterSel = GetSelectedOrderInListOption(cmbFilter);
            RefreshList(sortSel, filterSel);
        }

        private void BtnClearFilterSort_Click(object sender, RoutedEventArgs e)
        {
            cmbSort.SelectedItem = null;
            cmbFilter.SelectedItem = null;
            RefreshList();
        }

        // Opens OrderDetailsWindow when a delivery row is double-clicked.
        // Ensure the window shows the actual end-status of the delivery that was double-clicked.
        private void dgDeliveries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDeliveries.SelectedItem is not DeliveryView dv) return;

            try
            {
                var order = s_bl.Order.Details(_managerIdConfig, dv.OrderId);
                if (order == null)
                {
                    MessageBox.Show("Order details not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (dv.DeliveryId != 0 && order.Deliveries != null)
                {
                    var matching = order.Deliveries.FirstOrDefault(d => d.DeliveryId == dv.DeliveryId);
                    if (matching != null && matching.OrderStatus.HasValue)
                    {
                        order.OrderStatus = matching.OrderStatus.Value;
                    }
                }

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