using System;
using System.Windows;
using System.Threading.Tasks;
using BlApi;
using BO;
using System.Windows.Input; // Add this using directive at the top of the file

namespace PL.AvailableOrders
{
    /// <summary>
    /// Interaction logic for OrderDetailsWindow.xaml
    /// Reused for both read/view mode and add mode.
    /// </summary>
    public partial class OrderDetailsWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly int _orderId;
        private readonly bool _isManager;
        private readonly int _managerId;

        // Bindable visibility properties (no x:Name usage in XAML)
        public Visibility DeleteVisibility
        {
            get { return (Visibility)GetValue(DeleteVisibilityProperty); }
            set { SetValue(DeleteVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DeleteVisibilityProperty =
            DependencyProperty.Register(nameof(DeleteVisibility), typeof(Visibility), typeof(OrderDetailsWindow), new PropertyMetadata(Visibility.Collapsed));

        public Visibility AcceptVisibility
        {
            get { return (Visibility)GetValue(AcceptVisibilityProperty); }
            set { SetValue(AcceptVisibilityProperty, value); }
        }
        public static readonly DependencyProperty AcceptVisibilityProperty =
            DependencyProperty.Register(nameof(AcceptVisibility), typeof(Visibility), typeof(OrderDetailsWindow), new PropertyMetadata(Visibility.Visible));

        // New: indicates add-mode (true when creating a new order)
        public bool IsAddMode
        {
            get { return (bool)GetValue(IsAddModeProperty); }
            set { SetValue(IsAddModeProperty, value); }
        }

        public static readonly DependencyProperty IsAddModeProperty =
            DependencyProperty.Register(nameof(IsAddMode), typeof(bool), typeof(OrderDetailsWindow), new PropertyMetadata(false));

        // New: indicates whether fields should be editable (Add-mode OR manager + open/in-progress with active courier)
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(OrderDetailsWindow), new PropertyMetadata(false));

        public OrderDetailsWindow()
        {
            InitializeComponent();
        }

        // Construct with BO.Order (view-only helper)
        public OrderDetailsWindow(BO.Order order) : this()
        {
            DataContext = order;
            AcceptVisibility = Visibility.Collapsed;
            DeleteVisibility = Visibility.Collapsed;
            IsAddMode = false;
            // Viewing a BO.Order: not editable by default
            IsEditable = false;
        }

        // View existing order (used by manager / courier)
        public OrderDetailsWindow(int requesterId, int orderId, bool isManager = false) : this()
        {
            InitializeComponent();
            _requesterId = requesterId;
            _orderId = orderId;
            _isManager = isManager;
            _managerId = s_bl.Admin.GetConfig().ManagerId;

            IsAddMode = false;
            AcceptVisibility = _isManager ? Visibility.Collapsed : Visibility.Visible;
            LoadOrderDetails();

            Loaded += Window_Loaded;
            Closed += Window_Closed;
        }

        // New constructor: Add mode (reused details window for creating new order)
        // requesterId is the id of the manager performing the add (should be admin)
        public OrderDetailsWindow(int requesterId, bool isManager, bool isAddMode) : this()
        {
            InitializeComponent();
            _requesterId = requesterId;
            _isManager = isManager;
            _managerId = s_bl.Admin.GetConfig().ManagerId;
            IsAddMode = isAddMode;

            AcceptVisibility = Visibility.Collapsed;
            DeleteVisibility = Visibility.Collapsed;
            IsAddMode = isAddMode;

            // create an empty BO.Order for binding/editing
            var newOrder = new BO.Order
            {
                Id = 0,
                CreatedAt = DateTime.Now,
                FullAddress = string.Empty,
                CustomerName = string.Empty,
                CustomerPhone = string.Empty,
                OrderType = BO.OrderTypes.Food,
                Fragile = false,
                Weight = 1,
                Volume = 1,
                Description = string.Empty,
                Latitude = double.NaN,
                Longitude = double.NaN,
                AirDistance = 0
            };

            DataContext = newOrder;

            // Add-mode should be editable
            IsEditable = true;
            // Update button is not relevant in add-mode; remain collapsed

            // Add-mode doesn't need observers (no _orderId), but for consistency:
            // Don't register Loaded/Closed here since _orderId is 0
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try { s_bl.Order.AddObserver(_orderId, OrderObserver); } catch { }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Order.RemoveObserver(_orderId, OrderObserver); } catch { }
        }

        private void OrderObserver() => Dispatcher.Invoke(LoadOrderDetails);

        private void LoadOrderDetails()
        {
            try
            {
                var order = s_bl.Order.Details(s_bl.Admin.GetConfig().ManagerId, _orderId);
                DataContext = order;

                if (DataContext is BO.Order boOrder)
                {
                    DeleteVisibility = (_isManager && (boOrder.OrderStatus == OrderStatus.Open || boOrder.OrderStatus == OrderStatus.InProgress))
                        ? Visibility.Visible : Visibility.Collapsed;

                    AcceptVisibility = _isManager ? Visibility.Collapsed : Visibility.Visible;

                    // Determine whether any assigned courier is active (safe lookup)
                    bool hasActiveCourier = false;
                    try
                    {
                        if (boOrder.Deliveries != null)
                        {
                            foreach (var d in boOrder.Deliveries)
                            {
                                if (d.CourierId.HasValue)
                                {
                                    try
                                    {
                                        var courier = s_bl.Courier.Details(_managerId, d.CourierId.Value);
                                        if (courier != null && courier.IsActive)
                                        {
                                            hasActiveCourier = true;
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                        // ignore errors from courier lookup - treat as not active
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // any unexpected failure in checking deliveries -> treat as not having active courier
                        hasActiveCourier = false;
                    }

                    // Set editability: add-mode OR manager + (Open OR (InProgress AND assigned active courier))
                    IsEditable = IsAddMode || (_isManager && (boOrder.OrderStatus == OrderStatus.Open || (boOrder.OrderStatus == OrderStatus.InProgress && hasActiveCourier)));

                    // Show update button only to manager when editable
                    if (BtnUpdate != null)
                        BtnUpdate.Visibility = (_isManager && IsEditable) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isManager)
                {
                    MessageBox.Show("Managers cannot accept orders.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                s_bl.Order.ChooseOrder(_requesterId, _requesterId, _orderId);
                MessageBox.Show("Order Accepted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to accept order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Save new order handler (Add mode). Validates minimal fields then calls BL.Add
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAddMode) return;
            if (DataContext is not BO.Order newOrder)
            {
                MessageBox.Show("Invalid order data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(newOrder.FullAddress))
            {
                MessageBox.Show("Address is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(newOrder.CustomerName))
            {
                MessageBox.Show("Customer name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // ✅ Direct async call - no Task.Run needed
                await s_bl.Order.Add(_requesterId, newOrder);

                MessageBox.Show("Order added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (DO.BlInvalidValueException ex)
            {
                MessageBox.Show($"Address Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // Update (manager) - validate and call BL.UpdateDetails
        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditable) return;
            if (DataContext is not BO.Order boOrder) return;

            if (string.IsNullOrWhiteSpace(boOrder.FullAddress))
            {
                MessageBox.Show("Address is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                await s_bl.Order.UpdateDetails(_managerId, boOrder);

                MessageBox.Show("Order updated successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrderDetails();
            }
            catch (DO.BlInvalidValueException ex)
            {
                MessageBox.Show($"Address Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // ✅ Always resets cursor, even on exception
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BO.Order boOrder) return;

            if (!_isManager)
            {
                MessageBox.Show("Only manager can delete orders.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (boOrder.OrderStatus == OrderStatus.Closed || boOrder.OrderStatus == OrderStatus.Denied || boOrder.OrderStatus == OrderStatus.Cancelled)
            {
                MessageBox.Show("Order cannot be deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete order #{boOrder.Id}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                s_bl.Order.Cancel(_managerId, boOrder.Id);
                MessageBox.Show("Order deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}