using System;
using System.Windows;
using BlApi;
using BO;

namespace PL.Courier
{
    public partial class CourierWindow : Window
    {
        // Private field to access BL
        private static readonly IBl s_bl = Factory.Get();

        // ---------------------------------------------------------
        // Dependency Properties
        // ---------------------------------------------------------

        // The main object we are binding to
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
        public bool IsAddMode { get; set; }
        public string WindowTitle { get; set; }
        public string ButtonText { get; set; }

        // ---------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------
        public CourierWindow(int courierId = 0)
        {
            InitializeComponent();

            // Note: We do NOT write DataContext = this; here anymore.
            // It is defined in the XAML window tag.

            // Set UI Logic flags
            IsAddMode = (courierId == 0);
            WindowTitle = IsAddMode ? "Add New Courier" : "Update Courier Details";
            ButtonText = IsAddMode ? "Add" : "Update";

            // Initialize CurrentCourier based on the ID
            if (IsAddMode)
            {
                // Create new instance with defaults
                CurrentCourier = new BO.Courier
                {
                    Id = 0, // Will be filled by user
                    JoinDate = DateTime.Now,
                    IsActive = true,
                    Transport = BO.Transportation.Motorcycle,
                    DeliveryCountOnTime = 0,
                    DeliveryCountLate = 0
                };
            }
            else
            {
                // Fetch existing from BL
                try
                {
                    int managerId = s_bl.Admin.GetConfig().ManagerId;
                    CurrentCourier = s_bl.Courier.Details(managerId, courierId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading courier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close(); // Close window if we can't load the data
                }
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

                // Input Validation (Basic)
                if (string.IsNullOrWhiteSpace(CurrentCourier.FullName))
                    throw new Exception("Full Name is required.");

                if (IsAddMode)
                {
                    // Add Mode: BL.Add expects the object
                    if (CurrentCourier.Id <= 0) throw new Exception("ID must be positive.");

                    s_bl.Courier.Add(managerId, CurrentCourier);
                    MessageBox.Show("Courier added successfully!");
                }
                else
                {
                    // Update Mode: BL.Update expects the object
                    s_bl.Courier.UpdateDetails(managerId, CurrentCourier);
                    MessageBox.Show("Courier updated successfully!");
                }

                Close();
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