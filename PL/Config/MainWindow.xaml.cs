using BlApi;
using BO;
using PL.Courier;
using PL.Courier.ForManager;
using PL.Helpers;
using PL.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input; // Required for Cursors

namespace PL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Access to the Business Logic layer
        static readonly BlApi.IBl s_bl = BlApi.Factory.Get();
        
        // Changed from static field to instance property to get current value
        private int ManagerId => s_bl.Admin.GetConfig().ManagerId;

        #region Dependency Properties

        public BO.Config Configuration
        {
            get { return (BO.Config)GetValue(ConfigurationProperty); }
            set { SetValue(ConfigurationProperty, value); }
        }

        public static readonly DependencyProperty ConfigurationProperty =
            DependencyProperty.Register("Configuration", typeof(BO.Config), typeof(MainWindow));

        public DateTime CurrentClock
        {
            get { return (DateTime)GetValue(CurrentClockProperty); }
            set { SetValue(CurrentClockProperty, value); }
        }

        public static readonly DependencyProperty CurrentClockProperty =
            DependencyProperty.Register("CurrentClock", typeof(DateTime), typeof(MainWindow));

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Register for the screen load event
            Loaded += Window_Loaded;

            // Register for the screen close event
            Closed += Window_Closed;
        }

        /// <summary>
        /// Event handler for the Window Loaded event.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Set initial values
            CurrentClock = s_bl.Admin.GetClock();
            Configuration = s_bl.Admin.GetConfig();

            // 2. Register observers
            RegisterObservers();
        }

        /// <summary>
        /// Event handler for the Window Closed event.
        /// Prevents memory leaks by unregistering observers.
        /// </summary>
        private void Window_Closed(object? sender, EventArgs e)
        {
            //  Stop simulator on window close
            if (IsSimulatorRunning)
            {
                try
                {
                    s_bl.Admin.StopSimulator();
                    IsSimulatorRunning = false;
                }
                catch { }
            }

            UnregisterObservers();
        }

        /// <summary>
        /// Registers the observers with the BL.
        /// </summary>
        private void RegisterObservers()
        {
            s_bl.Admin.AddClockObserver(clockObserver);
            s_bl.Admin.AddConfigObserver(configObserver);
            s_bl.Admin.AddSimulatorObserver(simulatorObserver);
        }

        /// <summary>
        /// Unregisters the observers from the BL.
        /// </summary>
        private void UnregisterObservers()
        {
            s_bl.Admin.RemoveClockObserver(clockObserver);
            s_bl.Admin.RemoveConfigObserver(configObserver);
            s_bl.Admin.RemoveSimulatorObserver(simulatorObserver);
        }

        #region Observers
        private readonly ObserverMutex _clockMutex = new();
        private readonly ObserverMutex _configMutex = new();
        private void clockObserver()
        {
            // Check if already loading - if yes, mark restart needed and return
            if (_clockMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;

            // Queue the work on the UI thread (async)
            Dispatcher.BeginInvoke(async () =>
            {
                // Pulling the new time from BL
                CurrentClock = s_bl.Admin.GetClock();
                if (await _clockMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    clockObserver(); // Restart if needed
            });
        }

        private void configObserver()
        {
            // Check if already loading - if yes, mark restart needed and return
            if (_configMutex.CheckAndSetLoadInProgressOrRestartRequired())
                return;
            // Queue the work on the UI thread (async)
            Dispatcher.BeginInvoke(async() =>
            {
                Configuration = s_bl.Admin.GetConfig();
                if (await _configMutex.UnsetLoadInProgressAndCheckRestartRequested())
                    configObserver(); // Restart if needed
            });
        }

        private void simulatorObserver()
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsSimulatorRunning = s_bl.Admin.IsSimulatorRunning();
            });
        }

        #endregion

        #region Time Simulation Buttons

        private void btnAddOneMinute_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Admin.ForwardClock(BO.Times.Minute); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneHour_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Admin.ForwardClock(BO.Times.Hour); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneDay_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Admin.ForwardClock(BO.Times.Day); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneMonth_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Admin.ForwardClock(BO.Times.Month); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddOneYear_Click(object sender, RoutedEventArgs e)
        {
            try { s_bl.Admin.ForwardClock(BO.Times.Year); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Stage 7: Simulator Control

        /// <summary>
        /// Handles the Start/Stop Simulator button click.
        /// Toggles between starting and stopping the simulator.
        /// </summary>
        private void BtnSimulator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsSimulatorRunning)
                {
                    //  Start Simulator
                    if (Interval <= 0)
                    {
                        MessageBox.Show("Please enter a valid positive interval.",
                                        "Invalid Interval",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    s_bl.Admin.StartSimulator(Interval);
                    // do not set IsSimulatorRunning here; observer will do it when actually started
                }
                else
                {
                    //  Stop Simulator
                    s_bl.Admin.StopSimulator();
                    // do not set IsSimulatorRunning here; observer will do it when actually stopped
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulator error: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        #endregion

        #region Configuration Update

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // 1. Validate address not empty
                if (string.IsNullOrWhiteSpace(Configuration.CompanyAddress))
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Address cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Refresh to show last valid configuration
                    Configuration = s_bl.Admin.GetConfig();
                    return;
                }

                // 2. Get coordinates from BL (BL handles HTTP/json)
                var coords = await s_bl.Admin.GetCoordinatesFromAddressAsync(Configuration.CompanyAddress);

                Mouse.OverrideCursor = null; // restore cursor

                // 3. Critical check: did we find coordinates?
                if (coords.Lat == null || coords.Lon == null)
                {
                    MessageBox.Show("Cannot find this address on the map.\nPlease check spelling or try a more specific address (City, Street).",
                                    "Invalid Address",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    // Refresh to show last valid configuration
                    Configuration = s_bl.Admin.GetConfig();
                    return;
                }

                // 4. Update the configuration object
                Configuration.Latitude = coords.Lat.Value;
                Configuration.Longitude = coords.Lon.Value;

                // 5. Save via BL
                s_bl.Admin.SetConfig(Configuration);

                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("Error saving configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Refresh to show last valid configuration
                Configuration = s_bl.Admin.GetConfig();
            }
        }

        #endregion

        #region Database Management Events

        private void btnInitializeDB_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to initialize the database? This will overwrite existing data.",
                                "Confirm Initialization",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                PerformDbOperation(() => s_bl.Admin.InitializeDB());
            }
        }

        private void btnResetDB_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to RESET the database? All data will be lost.",
                                "Confirm Reset",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                PerformDbOperation(() => s_bl.Admin.ResetDB());
            }
        }

        private void PerformDbOperation(Action dbAction)
        {
            // 1. Close other windows
            List<Window> windowsToClose = new List<Window>();
            foreach (Window w in Application.Current.Windows)
            {
                if (w != this) windowsToClose.Add(w);
            }
            foreach (var w in windowsToClose) w.Close();

            // 2. Set Wait Cursor
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // 3. Perform BL Operation
                dbAction.Invoke();

                // 4. Update UI immediately
                CurrentClock = s_bl.Admin.GetClock();
                Configuration = s_bl.Admin.GetConfig();

                // 5. Clean up old observers before re-registering to avoid duplicates
                UnregisterObservers();
                RegisterObservers();

                MessageBox.Show("Operation completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        #endregion

        #region Navigation Events

        private void btnHandleCouriers_Click(object sender, RoutedEventArgs e)
        {
            new CourierListWindow().Show();
        }

        private void BtnAllOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new AvailableOrderListWindow(ManagerId, true) { Owner = this };
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Stage 7: Stop simulator before logout
            if (IsSimulatorRunning)
            {
                try
                {
                    s_bl.Admin.StopSimulator();
                    IsSimulatorRunning = false;
                }
                catch { }
            }

            LoginWindow.ShowSingle();
            Close(); // close ONLY MainWindow
        }
        private void BtnAllDeliveries_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new PL.Deliveries.DeliveriesListWindow(ManagerId, true);
            wnd.ShowDialog();
        }

        #endregion

        #region Simulator Properties (Stage 7)

        // Dependency Property for Interval
        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }
        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(nameof(Interval), typeof(int), typeof(MainWindow), new PropertyMetadata(1));

        // Dependency Property for IsSimulatorRunning (Flag)
        public bool IsSimulatorRunning
        {
            get { return (bool)GetValue(IsSimulatorRunningProperty); }
            set { SetValue(IsSimulatorRunningProperty, value); }
        }

        public static readonly DependencyProperty IsSimulatorRunningProperty =
            DependencyProperty.Register(nameof(IsSimulatorRunning), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        #endregion
    }
}