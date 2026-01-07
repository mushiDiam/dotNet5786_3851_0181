using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Input; // Required for Cursors
using System.Linq; // Required for window collection operations
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
        private static readonly System.Net.Http.HttpClient s_client = new System.Net.Http.HttpClient();

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

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // 1. בדיקה שהכתובת לא ריקה
                if (string.IsNullOrWhiteSpace(Configuration.CompanyAddress))
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Address cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. חיפוש קואורדינטות
                var coords = await GetCoordinatesFromAddressAsync(Configuration.CompanyAddress);

                Mouse.OverrideCursor = null; // החזרת הסמן הרגיל

                // 3. בדיקה קריטית: האם נמצאה כתובת?
                if (coords.Lat == null || coords.Lon == null)
                {
                    // --- עצירה! לא שומרים! ---
                    MessageBox.Show("Cannot find this address on the map.\nPlease check spelling or try a more specific address (City, Street).",
                                    "Invalid Address",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    return; // יציאה מהפונקציה בלי לשמור
                }

                // 4. אם הגענו לפה - הכתובת תקינה. נעדכן את האובייקט.
                Configuration.Latitude = coords.Lat.Value;
                Configuration.Longitude = coords.Lon.Value;

                // 5. שמירה ל-XML
                s_bl.Admin.SetConfig(Configuration);

                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
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

        private void BtnAllOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int managerId = s_bl.Admin.GetConfig().ManagerId;
                var wnd = new AvailableOrderListWindow(managerId, true) { Owner = this };
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // show login window first
                var login = new LoginWindow();
                login.Show();

                // close all other windows except the new login window
                var windowsToClose = Application.Current.Windows.Cast<Window>().Where(w => w != login).ToList();
                foreach (var w in windowsToClose)
                    w.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logout failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // Add this helper method to MainWindow class to resolve CS0103
        private async Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, null);

            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

            try
            {
                // --- תיקון השגיאה 403 Forbidden ---
                // 1. ניקוי כותרות קודמות כדי למנוע כפילויות שגורמות לשגיאות
                s_client.DefaultRequestHeaders.UserAgent.Clear();

                // 2. הוספת User-Agent שנראה כמו דפדפן אמיתי או אפליקציה לגיטימית
                // השרתים של OSM בדרך כלל מכבדים את הפורמט הזה:
                s_client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CouriersStudentProject/1.0");

                // 3. ביצוע הבקשה
                var response = await s_client.GetAsync(url);

                // אם עדיין יש שגיאה, נראה אותה ברור
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    MessageBox.Show("OpenStreetMap Blocked the request (403). Try again in a few seconds.");
                    return (null, null);
                }

                response.EnsureSuccessStatusCode(); // זורק חריגה אם יש שגיאה אחרת

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetArrayLength() > 0)
                {
                    var location = root[0];
                    if (double.TryParse(location.GetProperty("lat").GetString(), out double lat) &&
                        double.TryParse(location.GetProperty("lon").GetString(), out double lon))
                    {
                        return (lat, lon);
                    }
                }
            }
            catch (Exception ex)
            {
                // הצגת השגיאה המלאה לדיבוג
                MessageBox.Show($"Geocoding Error: {ex.Message}");
            }

            return (null, null);
        }
    }
}