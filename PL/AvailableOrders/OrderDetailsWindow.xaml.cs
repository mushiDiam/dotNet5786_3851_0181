using System;
using System.Windows;
using System.Threading.Tasks;
using BlApi;
using BO;

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

        // New: indicates add-mode (true when creating a new order)
        public bool IsAddMode
        {
            get { return (bool)GetValue(IsAddModeProperty); }
            set { SetValue(IsAddModeProperty, value); }
        }

        public static readonly DependencyProperty IsAddModeProperty =
            DependencyProperty.Register(nameof(IsAddMode), typeof(bool), typeof(OrderDetailsWindow), new PropertyMetadata(false));

        public OrderDetailsWindow()
        {
            InitializeComponent();
        }

        // Construct with BO.Order (view-only helper)
        public OrderDetailsWindow(BO.Order order) : this()
        {
            DataContext = order;
            BtnAccept.Visibility = Visibility.Collapsed;
            BtnSave.Visibility = Visibility.Collapsed;
            IsAddMode = false;
        }

        // View existing order (used by manager / courier)
        public OrderDetailsWindow(int requesterId, int orderId, bool isManager = false) : this()
        {
            InitializeComponent();
            _requesterId = requesterId;
            _orderId = orderId;
            _isManager = isManager;

            IsAddMode = false;

            // Logic: Managers should not Accept
            BtnAccept.Visibility = _isManager ? Visibility.Collapsed : Visibility.Visible;
            BtnSave.Visibility = Visibility.Collapsed;

            LoadOrderDetails();
        }

        // New constructor: Add mode (reused details window for creating new order)
        // requesterId is the id of the manager performing the add (should be admin)
        public OrderDetailsWindow(int requesterId, bool isManager, bool isAddMode) : this()
        {
            InitializeComponent();
            _requesterId = requesterId;
            _isManager = isManager;
            IsAddMode = isAddMode;

            // Add-mode specific UI
            BtnAccept.Visibility = Visibility.Collapsed;
            BtnSave.Visibility = IsAddMode ? Visibility.Visible : Visibility.Collapsed;

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
        }

        private void LoadOrderDetails()
        {
            try
            {
                int adminId = s_bl.Admin.GetConfig().ManagerId;
                var order = s_bl.Order.Details(adminId, _orderId);
                DataContext = order;
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
            try
            {
                if (!IsAddMode)
                    return;

                if (DataContext is not BO.Order newOrder)
                {
                    MessageBox.Show("Invalid order data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Basic validation (leave coordinates/distance resolution to BL)
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

                // Call BL — BL will resolve coordinates and compute air distance
                // Offload the synchronous BL call to the threadpool and await it.
                await Task.Run(() => s_bl.Order.Add(_requesterId, newOrder));

                MessageBox.Show("Order added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}