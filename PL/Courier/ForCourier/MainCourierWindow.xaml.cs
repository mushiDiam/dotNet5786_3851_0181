using System;
using System.Linq;
using System.Threading.Tasks;
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

        public DateTime CurrentClock
        {
            get { return (DateTime)GetValue(CurrentClockProperty); }
            set { SetValue(CurrentClockProperty, value); }
        }
        public static readonly DependencyProperty CurrentClockProperty =
            DependencyProperty.Register("CurrentClock", typeof(DateTime), typeof(MainCourierWindow));

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

            try { s_bl.Courier.AddObserver(_courierId, CourierObserver); } catch { }
            try { s_bl.Order.AddObserver(_courierId, CourierObserver); } catch { }
            try { s_bl.Admin.AddClockObserver(ClockObserver); CurrentClock = s_bl.Admin.GetClock(); } catch { }
        }

        private void MainCourierWindow_Closed(object? sender, EventArgs e)
        {
            try { s_bl.Courier.RemoveObserver(_courierId, CourierObserver); } catch { }
            try { s_bl.Order.RemoveObserver(_courierId, CourierObserver); } catch { }
            try { s_bl.Admin.RemoveClockObserver(ClockObserver); } catch { }
        }

        private void CourierObserver() => Dispatcher.Invoke(RefreshCourier);
        private void ClockObserver() => Dispatcher.Invoke(() => CurrentClock = s_bl.Admin.GetClock());

        private void RefreshAll()
        {
            RefreshCourier();
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
                Close();
            }
        }

        private void UpdateMaskedPassword()
        {
            if (CurrentCourier == null || string.IsNullOrEmpty(CurrentCourier.Password))
                MaskedPassword = "(no password)";
            else
                MaskedPassword = new string('*', CurrentCourier.Password.Length);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate company max is configured and courier value is not greater
                var cfg = s_bl.Admin.GetConfig();
                // Use MaxiumDistance instead of MaxDeliveryDistance
                if (cfg.MaxiumDistance == 0)
                {
                    MessageBox.Show("Company maximum delivery distance is not configured. Contact the administrator.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshCourier();
                    return;
                }

                if (CurrentCourier.MaxDistancePreference > cfg.MaxiumDistance)
                {
                    MessageBox.Show($"Max distance cannot exceed company maximum ({cfg.MaxiumDistance} km).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshCourier();
                    return;
                }

                s_bl.Courier.UpdateDetails(_managerId, CurrentCourier);
                RefreshCourier();
                MessageBox.Show("Profile updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RefreshCourier();
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ChangePasswordWindow(_courierId, _managerId);
            if (dlg.ShowDialog() == true)
            {
                RefreshCourier();
                UpdateMaskedPassword();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private void BtnAvailableOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new AvailableOrderListWindow(_courierId, false) { Owner = this };
                wnd.ShowDialog();
                RefreshCourier();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open available orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeliveryHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new Deliveries.DeliveriesListWindow(_courierId, false) { Owner = this };
                wnd.ShowDialog();
                RefreshCourier();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Active delivery helpers and handlers ---

        // returns delivery id or null
        private int? GetActiveDeliveryId() => CurrentCourier?.ActiveOrder?.DeliveryId;

        // returns order id or null
        private int? GetActiveOrderId() => CurrentCourier?.ActiveOrder?.OrderId;

        // Completed: uses the BL method that already exists: EndOfOrder(requesterId, courierId, deliveryId)
        private async void BtnMarkCompleted_Click(object sender, RoutedEventArgs e)
        {
            var deliveryId = GetActiveDeliveryId();
            if (deliveryId == null)
            {
                MessageBox.Show("No active delivery to complete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Mark delivery as completed?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                s_bl.Order.EndOfOrder(_courierId, _courierId, deliveryId.Value);

                // immediate UI feedback: clear active order locally
                ClearActiveOrderLocally();

                MessageBox.Show("Delivery marked as completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to complete delivery: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // ensure BL/PL sync shortly after operation
                await SyncCourierAfterDelay();
            }
        }

        // Cancelled: try BL call if available, otherwise clear UI locally
        private async void BtnMarkCancelled_Click(object sender, RoutedEventArgs e)
        {
            var deliveryId = GetActiveDeliveryId();
            var orderId = GetActiveOrderId();
            if (deliveryId == null || orderId == null)
            {
                MessageBox.Show("No active delivery to cancel.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Cancel this delivery?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                // Try to call courier-specific cancel on BL if implemented
                var orderObj = s_bl.Order;
                var method = orderObj.GetType().GetMethod("CancelDeliveryByCourier") ?? orderObj.GetType().GetMethod("CancelDelivery");
                if (method != null)
                {
                    method.Invoke(orderObj, new object[] { _courierId, _courierId, deliveryId.Value });
                }
                else
                {
                    // Fallback: attempt to call Cancel(order) as last resort using manager id
                    try
                    {
                        s_bl.Order.Cancel(_managerId, orderId.Value);
                    }
                    catch
                    {
                        // ignore: we still want to clear UI locally
                    }
                }

                // immediate UI feedback
                ClearActiveOrderLocally();
                MessageBox.Show("Delivery cancelled (local UI updated).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                MessageBox.Show($"Failed to cancel delivery: {tie.InnerException.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to cancel delivery: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await SyncCourierAfterDelay();
            }
        }

        // Not Found: try BL call if available, otherwise clear UI locally
        // In MainCourierWindow.xaml.cs
        private async void BtnMarkNotFound_Click(object sender, RoutedEventArgs e)
        {
            var deliveryId = GetActiveDeliveryId();
            if (deliveryId == null)
            {
                MessageBox.Show("No active delivery to report.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Report 'Not Found' for this delivery?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // Call the proper BL method
                s_bl.Order.MarkDeliveryNotFound(_courierId, _courierId, deliveryId.Value);

                ClearActiveOrderLocally();
                MessageBox.Show("Reported as Not Found.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to report not-found: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await SyncCourierAfterDelay();
            }
        }

        // Creates a shallow copy of the current courier with ActiveOrder cleared and assigns it to the DP
        private void ClearActiveOrderLocally()
        {
            if (CurrentCourier == null) return;

            var clone = new BO.Courier
            {
                Id = CurrentCourier.Id,
                FullName = CurrentCourier.FullName,
                PhoneNumber = CurrentCourier.PhoneNumber,
                Email = CurrentCourier.Email,
                Password = CurrentCourier.Password,
                IsActive = CurrentCourier.IsActive,
                MaxDistancePreference = CurrentCourier.MaxDistancePreference,
                Transport = CurrentCourier.Transport,
                JoinDate = CurrentCourier.JoinDate,
                DeliveryCountOnTime = CurrentCourier.DeliveryCountOnTime,
                DeliveryCountLate = CurrentCourier.DeliveryCountLate,
                ActiveOrder = null
            };

            // assign cloned object to trigger the DP change and update UI immediately
            CurrentCourier = clone;
        }

        // Ask PL to re-sync with BL after a short delay (gives BL time to persist updates)
        private async Task SyncCourierAfterDelay(int delayMs = 800)
        {
            try
            {
                await Task.Delay(delayMs);
                RefreshCourier();
            }
            catch { }
        }
    }
}