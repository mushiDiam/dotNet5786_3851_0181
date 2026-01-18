using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlApi;
using BO;
using PL.AvailableOrders;
using PL.Helpers;

namespace PL.Deliveries
{
    public partial class DeliveriesListWindow : Window, INotifyPropertyChanged
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly bool _isManager;
        private readonly int _managerIdConfig;

        // Stage 7: Add mutex
        private readonly ObserverMutex _deliveryListMutex = new();

        // --- Binding Properties ---

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // 1. DataGrid Source
        private IEnumerable<DeliveryView> _deliveryItems;
        public IEnumerable<DeliveryView> DeliveryItems
        {
            get => _deliveryItems;
            set { _deliveryItems = value; OnPropertyChanged(); }
        }

        // 2. DataGrid Selection
        private DeliveryView _selectedDelivery;
        public DeliveryView SelectedDelivery
        {
            get => _selectedDelivery;
            set { _selectedDelivery = value; OnPropertyChanged(); }
        }

        // 3. Summary Buttons Source
        private IEnumerable<SummaryItem> _summaryItems;
        public IEnumerable<SummaryItem> SummaryItems
        {
            get => _summaryItems;
            set { _summaryItems = value; OnPropertyChanged(); }
        }

        // 4. ComboBox Sources
        public IEnumerable<OrderInListOptions> SortOptionsList { get; } = Enum.GetValues(typeof(OrderInListOptions)).Cast<OrderInListOptions>();
        public IEnumerable<OrderInListOptions> FilterOptionsList { get; } = Enum.GetValues(typeof(OrderInListOptions)).Cast<OrderInListOptions>();

        // 5. ComboBox Selections (Trigger Refresh on change)
        private OrderInListOptions? _selectedSort;
        public OrderInListOptions? SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); RefreshList(); }
        }

        private OrderInListOptions? _selectedFilter;
        public OrderInListOptions? SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); RefreshList(); }
        }

        // --- View Models ---

        public class DeliveryView
        {
            public int DeliveryId { get; set; }
            public int OrderId { get; set; }
            public string CustomerName { get; set; }
            public string CourierName { get; set; }
            public string Status { get; set; }
            public DateTime? PickedUp { get; set; }
            public DateTime? Delivered { get; set; }
            public string Address { get; set; }
        }

        public class SummaryItem
        {
            public string Label { get; set; }
            public OrderStatus Status { get; set; }
            public ScheduleStatus SchedStatus { get; set; }
        }

        // --- Constructor & Initialization ---

        public DeliveriesListWindow(int requesterId, bool isManager)
        {
            InitializeComponent();
            DataContext = this; // Set DataContext for binding

            _requesterId = requesterId;
            _isManager = isManager;
            _managerIdConfig = s_bl.Admin.GetConfig().ManagerId;

            // Set default sort without triggering immediate refresh (optional, handled by Loaded)
            _selectedSort = OrderInListOptions.OrderId;

            // Events for Observer Pattern
            Loaded += Window_Loaded;
            Closed += Window_Closed;
        }

        // --- Observer Logic (Synchronization) ---

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            try { s_bl.Order.AddObserver(DeliveryListObserver); } catch { }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(DeliveryListObserver); } catch { }
        }

        // Stage 7: Updated observer
        private void DeliveryListObserver()
        {
            // Check and prevent double entry
            if (_deliveryListMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;

            // Queue work on UI thread
            Dispatcher.BeginInvoke(async () =>
            {
                // The actual work
                RefreshList();

                // Check if restart needed
                if (await _deliveryListMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    DeliveryListObserver();
            });
        }

        // --- Main Logic ---

        private void RefreshList()
        {
            try
            {
                // 1. Get raw orders
                var rawList = s_bl.Order.GetOrders(_managerIdConfig, null, null, null);
                var allOrders = new List<BO.Order>();

                // 2. Fetch full details (heavy operation, but required by logic provided)
                if (rawList != null)
                {
                    foreach (var item in rawList)
                    {
                        try
                        {
                            var full = s_bl.Order.Details(_managerIdConfig, item.OrderId);
                            if (full != null) allOrders.Add(full);
                        }
                        catch { /* Ignore deleted orders during loop */ }
                    }
                }

                // 3. Filter by User Role
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

                var filteredList = filteredOrders.ToList();

                // 4. Update Summary (Top Section)
                UpdateSummary(filteredList);

                // 5. Map to DeliveryView
                var query = filteredList.Select(o =>
                {
                    BO.DeliveryPerOrderInList? relevantDelivery = null;

                    if (_isManager)
                        relevantDelivery = o.Deliveries?.LastOrDefault();
                    else
                        relevantDelivery = o.Deliveries?.Where(d => d.CourierId == _requesterId).LastOrDefault()
                                         ?? o.Deliveries?.LastOrDefault();

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

                // 6. Apply UI Filtering
                if (SelectedFilter.HasValue)
                {
                    switch (SelectedFilter.Value)
                    {
                        case OrderInListOptions.DeliveryId:
                            query = query.Where(x => x.DeliveryId != 0); break;
                        case OrderInListOptions.OrderStatus:
                            query = query.Where(x => !string.IsNullOrWhiteSpace(x.Status)); break;
                        case OrderInListOptions.CompletionTime:
                            query = query.Where(x => x.Delivered.HasValue); break;
                    }
                }

                // 7. Apply UI Sorting
                if (SelectedSort.HasValue)
                {
                    switch (SelectedSort.Value)
                    {
                        case OrderInListOptions.DeliveryId:
                            query = query.OrderByDescending(x => x.DeliveryId); break;
                        case OrderInListOptions.OrderId:
                            query = query.OrderByDescending(x => x.OrderId); break;
                        case OrderInListOptions.OrderStatus:
                            query = query.OrderBy(x => x.Status); break;
                        case OrderInListOptions.CompletionTime:
                            query = query.OrderBy(x => x.Delivered); break;
                        default:
                            query = query.OrderByDescending(x => x.OrderId); break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.OrderId);
                }

                // 8. Update Bound Property
                DeliveryItems = query.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load deliveries: {ex.Message}");
            }
        }

        private void UpdateSummary(List<BO.Order> orders)
        {
            var newSummary = new List<SummaryItem>();
            var orderStatuses = Enum.GetValues(typeof(BO.OrderStatus)).Cast<BO.OrderStatus>();
            var scheduleStatuses = Enum.GetValues(typeof(BO.ScheduleStatus)).Cast<BO.ScheduleStatus>();

            foreach (var os in orderStatuses)
            {
                foreach (var ss in scheduleStatuses)
                {
                    int count = orders.Count(o => o.OrderStatus == os && o.ScheduleStatus == ss);
                    if (count > 0)
                    {
                        newSummary.Add(new SummaryItem
                        {
                            Label = $"{os} / {ss}: {count}",
                            Status = os,
                            SchedStatus = ss
                        });
                    }
                }
            }
            SummaryItems = newSummary;
        }

        // --- Event Handlers ---

        // Button in Summary ItemsControl
        private void SummaryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SummaryItem item)
            {
                try
                {
                    var wnd = new AvailableOrderListWindow(_managerIdConfig, true, item.Status, item.SchedStatus)
                    {
                        Owner = this
                    };
                    wnd.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening list: {ex.Message}");
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshList();

        private void BtnClearFilterSort_Click(object sender, RoutedEventArgs e)
        {
            // Resetting properties triggers refresh via setter
            SelectedSort = null;
            SelectedFilter = null;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void dgDeliveries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedDelivery == null) return;

            try
            {
                var order = s_bl.Order.Details(_managerIdConfig, SelectedDelivery.OrderId);
                if (order == null) return;

                // Attempt to match status to specific delivery
                if (SelectedDelivery.DeliveryId != 0 && order.Deliveries != null)
                {
                    var matching = order.Deliveries.FirstOrDefault(d => d.DeliveryId == SelectedDelivery.DeliveryId);
                    if (matching != null && matching.OrderStatus.HasValue)
                    {
                        order.OrderStatus = matching.OrderStatus.Value;
                    }
                }

                var wnd = new OrderDetailsWindow(order) { Owner = this };
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error details: {ex.Message}");
            }
        }
    }
}