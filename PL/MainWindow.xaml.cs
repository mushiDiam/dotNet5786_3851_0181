using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input; // Required for Cursors
using BlApi;
using BO;
using PL.Courier;

namespace PL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Access to the Business Logic layer
        static readonly BlApi.IBl s_bl = BlApi.Factory.Get();

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
            UnregisterObservers();
        }

        /// <summary>
        /// Registers the observers with the BL.
        /// </summary>
        private void RegisterObservers()
        {
            s_bl.Admin.AddClockObserver(clockObserver);
            s_bl.Admin.AddConfigObserver(configObserver);
        }

        /// <summary>
        /// Unregisters the observers from the BL.
        /// </summary>
        private void UnregisterObservers()
        {
            s_bl.Admin.RemoveClockObserver(clockObserver);
            s_bl.Admin.RemoveConfigObserver(configObserver);
        }

        #region Observers

        private void clockObserver()
        {
            Dispatcher.Invoke(() =>
            {
                // Pulling the new time from BL
                CurrentClock = s_bl.Admin.GetClock();
            });
        }

        private void configObserver()
        {
            Dispatcher.Invoke(() =>
            {
                Configuration = s_bl.Admin.GetConfig();
            });
        }

        #endregion

        #region Time Simulation Buttons

        private void btnAddOneMinute_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.Times.Minute);
        }

        private void btnAddOneHour_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.Times.Hour);
        }

        private void btnAddOneDay_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.Times.Day);
        }

        private void btnAddOneMonth_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.Times.Month);
        }

        private void btnAddOneYear_Click(object sender, RoutedEventArgs e)
        {
            s_bl.Admin.ForwardClock(BO.Times.Year);
        }

        #endregion

        #region Configuration Update

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                s_bl.Admin.SetConfig(Configuration);
                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // 5. FIX: Clean up old observers before re-registering to avoid duplicates
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

        #endregion
    }
}