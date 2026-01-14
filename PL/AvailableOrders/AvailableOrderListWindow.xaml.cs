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

        // Optional pre-filters (used when opened from summary)
        private readonly OrderStatus? _filterOrderStatus;
        private readonly ScheduleStatus? _filterScheduleStatus;

        // Property for Binding to the View
        // Remove the property declaration for CourierVisibility at the top of the class:
     

        // Keep only the property with the getter below, which avoids the ambiguity:
        public Visibility CourierVisibility => IsManager ? Visibility.Collapsed : Visibility.Visible;

        private sealed class OrderView
        {
            public int OrderId { get; init; }
            public string CustomerName { get; init; } = "";
            public string FullAddress { get; init; } = "";
            public double AirDistance { get; init; }
            public BO.ScheduleStatus? ScheduleStatus { get; init; }
            public TimeSpan? RemainingTime { get; init; }
            public BO.OrderStatus OrderStatus { get; init; } // used by template triggers
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

        // Exposed to XAML binding: column will be Visible only for managers
        public Visibility ManagerVisibility => IsManager ? Visibility.Visible : Visibility.Collapsed;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AvailableOrderListWindow(int requesterId, bool isManager = false, OrderStatus? orderStatusFilter = null, ScheduleStatus? scheduleStatusFilter = null)
        {
            // Set filters before InitializeComponent so RefreshList can use them when Loaded triggers
            _filterOrderStatus = orderStatusFilter;
            _filterScheduleStatus = scheduleStatusFilter;

            InitializeComponent();
            DataContext = this;
            _requesterId = requesterId;
            _managerId = s_bl.Admin.GetConfig().ManagerId;
            IsManager = isManager;

            Loaded += AvailableOrderListWindow_Loaded;
            Closed += AvailableOrderListWindow_Closed;
        }

        private void AvailableOrderListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            try { s_bl.Order.AddObserver(OrderObserver); } catch { }
        }

        private void AvailableOrderListWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(OrderObserver); } catch { }
        }

        private void OrderObserver()
        {
            Dispatcher.Invoke(RefreshList);
        }

        private void RefreshList()
        {
            try
            {
                var cancelColumn = dgOrders.Columns.FirstOrDefault(c => c.Header.ToString() == "Cancel");
                if (cancelColumn != null)
                {
                    cancelColumn.Visibility = IsManager ? Visibility.Visible : Visibility.Collapsed;
                }
                if (IsManager)
                {
                    var orders = s_bl.Order.GetOrders(_managerId, null, null, null)?.ToList() ?? new List<BO.OrderInList>();
                    var filtered = orders.Where(o =>
                        (!_filterOrderStatus.HasValue || o.OrderStatus == _filterOrderStatus.Value) &&
                        (!_filterScheduleStatus.HasValue || o.ScheduleStatus == _filterScheduleStatus.Value))
                        .ToList();

                    var list = filtered.Select(o => new OrderView {
                        OrderId = o.OrderId,
                        AirDistance = o.AirDistance,
                        ScheduleStatus = o.ScheduleStatus,
                        RemainingTime = o.RemainingTime,
                        OrderStatus = o.OrderStatus,
                        CustomerName = "" }).ToList();

                    dgOrders.ItemsSource = list;
                }
                else
                {
                    var openFromBl = s_bl.Order.GetOpenOrder(_managerId, _requesterId, null, null)?.ToList() ?? new List<BO.OpenOrderInList>();
                    var filtered = openFromBl.Where(o =>
                        (!_filterOrderStatus.HasValue) &&
                        (!_filterScheduleStatus.HasValue || o.ScheduleStatus == _filterScheduleStatus.Value))
                        .ToList();

                    var blList = filtered.Select(o => new OrderView {
                        OrderId = o.OrderId,
                        FullAddress = o.FullAddress ?? "",
                        AirDistance = o.AirDistance,
                        ScheduleStatus = o.ScheduleStatus,
                        RemainingTime = o.RemainingTime,
                        OrderStatus = OrderStatus.Open,
                        CustomerName = "" }).ToList();

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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOrders.SelectedItem is not OrderView sel) return;

            try
            {
                var detailsWindow = new PL.AvailableOrders.OrderDetailsWindow(_requesterId, sel.OrderId, IsManager) { Owner = this };
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open order details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_requesterId, OrderObserver); } catch { }
            Close();
        }

        private void BtnChoose_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem is not OrderView sel)
            {
                MessageBox.Show("Please select an order first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                if (IsManager)
                {
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

        // New: per-row Cancel handler (manager-only)
        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not OrderView view) return;

            if (!IsManager)
            {
                MessageBox.Show("Only manager can cancel orders.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Only allow cancelling Open or InProgress (guard, although button visibility already enforces this)
            if (view.OrderStatus != OrderStatus.Open && view.OrderStatus != OrderStatus.InProgress)
            {
                MessageBox.Show("Order cannot be cancelled.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete Order #{view.OrderId}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                s_bl.Order.Cancel(_managerId, view.OrderId);
                MessageBox.Show($"Order #{view.OrderId} cancelled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to cancel order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new OrderDetailsWindow(_managerId, true, true)
                {
                    Owner = this
                };
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Add Order window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}