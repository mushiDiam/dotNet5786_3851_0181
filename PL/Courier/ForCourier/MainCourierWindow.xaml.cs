using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BlApi;
using BO;
using PL.Login;

namespace PL.Courier.ForCourier
{
    /// <summary>
    /// Interaction logic for MainCourierWindow.xaml
    /// </summary>
    public partial class MainCourierWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _courierId;
        private readonly int _managerId;

        // Dependency / Bindable properties
        public BO.Courier CurrentCourier
        {
            get { return (BO.Courier)GetValue(CurrentCourierProperty); }
            set
            {
                SetValue(CurrentCourierProperty, value);
                UpdateMaskedPassword();
            }
        }
        public static readonly DependencyProperty CurrentCourierProperty =
            DependencyProperty.Register(nameof(CurrentCourier), typeof(BO.Courier), typeof(MainCourierWindow), new PropertyMetadata(null));

        public IEnumerable<BO.OpenOrderInList> OpenOrders
        {
            get { return (IEnumerable<BO.OpenOrderInList>)GetValue(OpenOrdersProperty); }
            set { SetValue(OpenOrdersProperty, value); }
        }
        public static readonly DependencyProperty OpenOrdersProperty =
            DependencyProperty.Register(nameof(OpenOrders), typeof(IEnumerable<BO.OpenOrderInList>), typeof(MainCourierWindow), new PropertyMetadata(null));

        public BO.OpenOrderInList? SelectedOpenOrder { get; set; }

        public IEnumerable<BO.ClosedDeliveryInList> EndedDeliveries
        {
            get { return (IEnumerable<BO.ClosedDeliveryInList>)GetValue(EndedDeliveriesProperty); }
            set { SetValue(EndedDeliveriesProperty, value); }
        }
        public static readonly DependencyProperty EndedDeliveriesProperty =
            DependencyProperty.Register(nameof(EndedDeliveries), typeof(IEnumerable<BO.ClosedDeliveryInList>), typeof(MainCourierWindow), new PropertyMetadata(null));

        public DateTime CurrentClock
        {
            get { return (DateTime)GetValue(CurrentClockProperty); }
            set { SetValue(CurrentClockProperty, value); }
        }
        public static readonly DependencyProperty CurrentClockProperty =
            DependencyProperty.Register("CurrentClock", typeof(DateTime), typeof(MainCourierWindow));

        public BO.ClosedDeliveryInList? SelectedEndedDelivery { get; set; }

        // Masked password display
        public string MaskedPassword
        {
            get { return (string)GetValue(MaskedPasswordProperty); }
            set { SetValue(MaskedPasswordProperty, value); }
        }
        public static readonly DependencyProperty MaskedPasswordProperty =
            DependencyProperty.Register(nameof(MaskedPassword), typeof(string), typeof(MainCourierWindow), new PropertyMetadata(""));

        public MainCourierWindow(int courierId)
        {
            InitializeComponent();
            DataContext = this;

            _courierId = courierId;
            _managerId = s_bl.Admin.GetConfig().ManagerId;

            Loaded += MainCourierWindow_Loaded;
            Closed += MainCourierWindow_Closed;
        }

        private void MainCourierWindow_Loaded(object sender, EventArgs e)
        {
            RefreshAll();

            // Register courier observer to keep UI in sync if courier changed elsewhere
            try
            {
                s_bl.Courier.AddObserver(_courierId, CourierObserver);
            }
            catch { /* optional: BL may not implement per-courier observer; ignore safely */ }

            // Try to observe orders if available (best-effort)
            try
            {
                s_bl.Order.AddObserver(_courierId, OrderObserver);
            }
            catch { }

            // subscribe to admin clock if available (already implemented elsewhere)
            try { s_bl.Admin.AddClockObserver(clockObserver); CurrentClock = s_bl.Admin.GetClock(); } catch { }
        }

        private void MainCourierWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Courier.RemoveObserver(_courierId, CourierObserver); } catch { }
            try { s_bl.Order.RemoveObserver(_courierId, OrderObserver); } catch { }
            try { s_bl.Admin.RemoveClockObserver(clockObserver); } catch { }
        }

        private void CourierObserver()
        {
            Dispatcher.Invoke(() => RefreshCourier());
        }

        private void OrderObserver()
        {
            Dispatcher.Invoke(() => RefreshOrders());
        }

        private void clockObserver()
        {
            Dispatcher.Invoke(() => CurrentClock = s_bl.Admin.GetClock());
        }

        private void RefreshAll()
        {
            RefreshCourier();
            RefreshOrders();
            RefreshEndedDeliveries();
            try { CurrentClock = s_bl.Admin.GetClock(); } catch { }
        }

        private void RefreshCourier()
        {
            try
            {
                CurrentCourier = s_bl.Courier.Details(_managerId, _courierId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed loading courier info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMaskedPassword()
        {
            if (CurrentCourier == null || string.IsNullOrEmpty(CurrentCourier.Password))
                MaskedPassword = "(no password)";
            else
                MaskedPassword = new string('*', CurrentCourier.Password.Length);
        }

        private void RefreshOrders()
        {
            try
            {
                var open = s_bl.Order.GetOpenOrder(_managerId, _courierId, null, null);
                OpenOrders = open ?? new List<BO.OpenOrderInList>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed loading open orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OpenOrders = new List<BO.OpenOrderInList>();
            }
        }

        private void RefreshEndedDeliveries()
        {
            try
            {
                var closed = s_bl.Order.GetEndedDeliveries(_managerId, _courierId, null, null);
                EndedDeliveries = closed ?? new List<BO.ClosedDeliveryInList>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed loading delivery history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                EndedDeliveries = new List<BO.ClosedDeliveryInList>();
            }
        }

        // Save updated courier info
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prevent changing JoinDate/IsActive on UI by design — BL still requires full BO
                s_bl.Courier.UpdateDetails(_managerId, CurrentCourier);

                // Refresh to pick up persisted values (including password if changed)
                RefreshCourier();
                RefreshAll();
                MessageBox.Show("Profile updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

 

        // Change password opens modal dialog
        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ChangePasswordWindow(_courierId, _managerId);
            if (dlg.ShowDialog() == true)
            {
                // after successful password change, reload courier and update mask
                RefreshCourier();
                UpdateMaskedPassword();
            }
        }

        // Choose order handlers (unchanged)
        private void BtnChooseOrder_Click(object sender, RoutedEventArgs e)
        {
            ChooseSelectedOrder();
        }

        private void OpenOrders_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChooseSelectedOrder();
        }

        private void ChooseSelectedOrder()
        {
            if (SelectedOpenOrder == null)
            {
                MessageBox.Show("Select an order first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                s_bl.Order.ChooseOrder(_managerId, _courierId, (int)SelectedOpenOrder.CourierId);
                MessageBox.Show($"You chose order #{SelectedOpenOrder.CourierId}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh to update assigned orders and current delivery
                RefreshOrders();
                RefreshEndedDeliveries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to choose order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Logout (unchanged)
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var login = new LoginWindow();
                login.Show();

                var windowsToClose = Application.Current.Windows.Cast<Window>().Where(w => w != login).ToList();
                foreach (var w in windowsToClose)
                    w.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logout failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Open AvailableOrderListWindow for this courier
        private void BtnAvailableOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new AvailableOrderListWindow(_courierId, false) { Owner = this };
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open available orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
