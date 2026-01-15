using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using BlApi;
using BO;
using PL.AvailableOrders;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PL
{
    public partial class AvailableOrderListWindow : Window, INotifyPropertyChanged
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly int _managerId;

        private readonly OrderStatus? _filterOrderStatus;
        private readonly ScheduleStatus? _filterScheduleStatus;

        // --- Data Sources for Binding ---
        public enum SortOption { None, OrderId, Status, Distance }
        public IEnumerable<SortOption> SortOptionsList { get; } = Enum.GetValues(typeof(SortOption)).Cast<SortOption>();
        public IEnumerable<BO.OrderStatus> StatusOptionsList { get; } = Enum.GetValues(typeof(BO.OrderStatus)).Cast<BO.OrderStatus>();

        // --- Binding Properties ---
        private IEnumerable<OrderView> _ordersList;
        public IEnumerable<OrderView> OrdersList
        {
            get => _ordersList;
            set { _ordersList = value; OnPropertyChanged(); }
        }

        private OrderView _selectedOrder;
        public OrderView SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        private SortOption _selectedSort = SortOption.None;
        public SortOption SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); _ = RefreshListAsync(); }
        }

        private BO.OrderStatus? _selectedFilterStatus;
        public BO.OrderStatus? SelectedFilterStatus
        {
            get => _selectedFilterStatus;
            set { _selectedFilterStatus = value; OnPropertyChanged(); _ = RefreshListAsync(); }
        }

        // --- Visibility Properties ---
        public Visibility CourierVisibility => IsManager ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ManagerVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;

        // View Model
        public class OrderView
        {
            public int OrderId { get; init; }
            public string CustomerName { get; init; } = "";
            public string FullAddress { get; init; } = "";
            public double AirDistance { get; init; }
            public BO.ScheduleStatus? ScheduleStatus { get; init; }
            public TimeSpan? RemainingTime { get; init; }
            public BO.OrderStatus OrderStatus { get; init; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isManager;
        public bool IsManager
        {
            get => _isManager;
            set
            {
                if (_isManager == value) return;
                _isManager = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ManagerVisibility));
                OnPropertyChanged(nameof(CourierVisibility));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AvailableOrderListWindow(int requesterId, bool isManager = false, OrderStatus? orderStatusFilter = null, ScheduleStatus? scheduleStatusFilter = null)
        {
            _filterOrderStatus = orderStatusFilter;
            _filterScheduleStatus = scheduleStatusFilter;

            InitializeComponent();
            DataContext = this;
            _requesterId = requesterId;
            _managerId = s_bl.Admin.GetConfig().ManagerId;
            IsManager = isManager;

            // Apply pre-filter if passed in constructor
            if (_filterOrderStatus.HasValue)
                _selectedFilterStatus = _filterOrderStatus.Value;

            Loaded += AvailableOrderListWindow_Loaded;
            Closed += AvailableOrderListWindow_Closed;
        }

        private async void AvailableOrderListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshListAsync();
            try { s_bl.Order.AddObserver(OrderObserver); } catch { }
        }

        private void AvailableOrderListWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(OrderObserver); } catch { }
        }

        private void OrderObserver() => Dispatcher.Invoke(async () => await RefreshListAsync());

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Resetting properties triggers RefreshList via the setters
            SelectedSort = SortOption.None;
            SelectedFilterStatus = null;
        }

        // ✅ NEW: Async version that uses GetOrdersAsync
        private async Task RefreshListAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                IEnumerable<OrderView> list;

                if (IsManager)
                {
                    // 1. Create a filter based on current UI selection
                    BO.OrderInListOptions? blFilter = null;
                    object? blFilterValue = null;

                    if (SelectedFilterStatus.HasValue)
                    {
                        blFilter = BO.OrderInListOptions.OrderStatus;
                        blFilterValue = SelectedFilterStatus.Value;
                    }

                    // 2. Call BL WITH the filter - gets real driving distance!
                    var orders = (await s_bl.Order.GetOrdersAsync(_managerId, blFilter, blFilterValue, null))?.ToList()
                                 ?? new List<BO.OrderInList>();

                    // 3. For managers, only show Open and InProgress orders
                    //    (unless a specific status filter was already applied)
                    if (!SelectedFilterStatus.HasValue)
                    {
                        orders = orders.Where(o => o.OrderStatus == BO.OrderStatus.Open ||
                                                   o.OrderStatus == BO.OrderStatus.InProgress).ToList();
                    }

                    // 4. Apply constructor-level filters (if any)
                    var filtered = orders.Where(o =>
                        (!_filterOrderStatus.HasValue || o.OrderStatus == _filterOrderStatus.Value) &&
                        (!_filterScheduleStatus.HasValue || o.ScheduleStatus == _filterScheduleStatus.Value))
                        .ToList();

                    // 5. Fetch full BO.Order details per order
                    var detailsCache = new Dictionary<int, BO.Order?>(filtered.Count);
                    list = filtered.Select(o =>
                    {
                        BO.Order? boOrder = null;
                        try
                        {
                            if (!detailsCache.TryGetValue(o.OrderId, out boOrder))
                            {
                                boOrder = s_bl.Order.Details(_managerId, o.OrderId);
                                detailsCache[o.OrderId] = boOrder;
                            }
                        }
                        catch
                        {
                            boOrder = null;
                            detailsCache[o.OrderId] = null;
                        }

                        return new OrderView
                        {
                            OrderId = o.OrderId,
                            CustomerName = boOrder?.CustomerName ?? string.Empty,
                            FullAddress = boOrder?.FullAddress ?? string.Empty,
                            AirDistance = o.AirDistance, // ✅ Now contains real driving distance!
                            ScheduleStatus = o.ScheduleStatus,
                            RemainingTime = o.RemainingTime,
                            OrderStatus = o.OrderStatus
                        };
                    });
                }
                else
                {
                    // Courier view - uses GetOpenOrder (still sync)
                    var openFromBl = s_bl.Order.GetOpenOrder(_managerId, _requesterId, null, null)?.ToList() 
                                     ?? new List<BO.OpenOrderInList>();

                    var filtered = openFromBl.Where(o =>
                        (!SelectedFilterStatus.HasValue || SelectedFilterStatus.Value == OrderStatus.Open) &&
                        (!_filterOrderStatus.HasValue) &&
                        (!_filterScheduleStatus.HasValue || o.ScheduleStatus == _filterScheduleStatus.Value))
                        .ToList();

                    list = filtered.Select(o => new OrderView
                    {
                        OrderId = o.OrderId,
                        FullAddress = o.FullAddress ?? "",
                        AirDistance = o.AirDistance,
                        ScheduleStatus = o.ScheduleStatus,
                        RemainingTime = o.RemainingTime,
                        OrderStatus = OrderStatus.Open,
                        CustomerName = ""
                    });
                }

                // Sort
                list = SelectedSort switch
                {
                    SortOption.OrderId => list.OrderBy(o => o.OrderId),
                    SortOption.Status => list.OrderBy(o => o.OrderStatus),
                    SortOption.Distance => list.OrderBy(o => o.AirDistance),
                    _ => list.OrderByDescending(o => o.OrderId)
                };

                OrdersList = list.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OrdersList = new List<OrderView>();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await RefreshListAsync();

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // We can use the sender (DataGrid) or the Bound Property
            if (SelectedOrder == null) return;

            try
            {
                var detailsWindow = new PL.AvailableOrders.OrderDetailsWindow(_requesterId, SelectedOrder.OrderId, IsManager) { Owner = this };
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_requesterId, OrderObserver); } catch { }
            Close();
        }

        private async void BtnChoose_Click(object sender, RoutedEventArgs e)
        {
            // Use Bound Property instead of accessing DataGrid
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (IsManager)
                {
                    MessageBox.Show("Managers cannot choose orders.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int orderId = SelectedOrder.OrderId;
                s_bl.Order.ChooseOrder(_requesterId, _requesterId, orderId);
                MessageBox.Show($"You chose order #{orderId}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to choose order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not OrderView view) return;

            if (!IsManager)
            {
                MessageBox.Show("Unauthorized.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Delete Order #{view.OrderId}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                s_bl.Order.Cancel(_managerId, view.OrderId);
                MessageBox.Show("Order cancelled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add Order Button Click (Manager Only)
        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new OrderDetailsWindow(_managerId, true, true) { Owner = this }.Show();
            }
            catch { }
        }
    }
}