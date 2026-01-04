using System;
using System.Windows;
using BlApi;
using BO;

namespace PL.Courier
{
    public partial class CourierWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();

        // ---------------------------------------------------------
        // Dependency Properties
        // ---------------------------------------------------------

        public BO.Courier CurrentCourier
        {
            get { return (BO.Courier)GetValue(CurrentCourierProperty); }
            set { SetValue(CurrentCourierProperty, value); }
        }

        public static readonly DependencyProperty CurrentCourierProperty =
            DependencyProperty.Register(nameof(CurrentCourier), typeof(BO.Courier), typeof(CourierWindow), new PropertyMetadata(null));

        // ---------------------------------------------------------
        // UI Logic Properties
        // ---------------------------------------------------------

        // Used to enable/disable the DatePicker
        public bool IsAddMode
        {
            get { return (bool)GetValue(IsAddModeProperty); }
            set { SetValue(IsAddModeProperty, value); }
        }

        public static readonly DependencyProperty IsAddModeProperty =
            DependencyProperty.Register("IsAddMode", typeof(bool), typeof(CourierWindow), new PropertyMetadata(false));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(CourierWindow), new PropertyMetadata(""));

        // WindowTitle as DependencyProperty
        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }

        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(CourierWindow), new PropertyMetadata(""));

        // ---------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------
        public CourierWindow(int courierId = 0)
        {
            InitializeComponent();

            // Set Mode Logic
            IsAddMode = (courierId == 0);
            WindowTitle = IsAddMode ? "Add New Courier" : "Update Courier";
            ButtonText = IsAddMode ? "Add" : "Update";

            // Initialize Data
            if (IsAddMode)
            {
                CurrentCourier = new BO.Courier
                {
                    Id = 0,
                    JoinDate = DateTime.Now,
                    IsActive = true,
                    Transport = BO.Transportation.Motorcycle
                };
            }
            else
            {
                try
                {
                    int managerId = s_bl.Admin.GetConfig().ManagerId;
                    CurrentCourier = s_bl.Courier.Details(managerId, courierId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading courier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
        }
        // ---------------------------------------------------------
        // Observer Implementation
        // ---------------------------------------------------------

        /// <summary>
        /// This method is called automatically by the BL whenever the specific courier changes.
        /// </summary>
        private void CourierObserver()
        {
            // Must use Dispatcher because the BL event comes from a background thread
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (IsAddMode || CurrentCourier == null) return;

                    int id = CurrentCourier.Id;
                    int managerId = s_bl.Admin.GetConfig().ManagerId;
                    var updatedCourier = s_bl.Courier.Details(managerId, id);
                    CurrentCourier = updatedCourier;
                }
                catch (Exception)
                {
                    MessageBox.Show("This courier was deleted by another user.", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                }
            });
        }

        // ---------------------------------------------------------
        // Window Lifecycle Events
        // ---------------------------------------------------------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsAddMode && CurrentCourier != null && CurrentCourier.Id != 0)
            {
                try
                {

                    s_bl.Courier.AddObserver(CurrentCourier.Id, CourierObserver);
                }
                catch { }
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            if (!IsAddMode && CurrentCourier != null && CurrentCourier.Id != 0)
            {
                try
                {
                    s_bl.Courier.RemoveObserver(CurrentCourier.Id, CourierObserver);
                }
                catch { }
            }
        }

        // ---------------------------------------------------------
        // Event Handlers
        // ---------------------------------------------------------

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int managerId = s_bl.Admin.GetConfig().ManagerId;

                if (ButtonText == "Add")
                {
                    s_bl.Courier.Add(managerId, CurrentCourier);
                    MessageBox.Show("Courier added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    s_bl.Courier.UpdateDetails(managerId, CurrentCourier);
                    MessageBox.Show("Courier updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}