using System;
using System.Windows;
using BlApi;
using BO;

namespace PL.Courier
{
    public partial class CourierWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();

        // The object holding MUTABLE data (Name, Email, etc.)
        public BO.Courier CurrentCourier
        {
            get { return (BO.Courier)GetValue(CurrentCourierProperty); }
            set { SetValue(CurrentCourierProperty, value); }
        }
        public static readonly DependencyProperty CurrentCourierProperty =
            DependencyProperty.Register("CurrentCourier", typeof(BO.Courier), typeof(CourierWindow));

        // ---------------------------------------------------------
        // "Init" Properties (Handled separately because they are immutable)
        // ---------------------------------------------------------
        public int FixedId { get; set; }
        public DateTime FixedJoinDate { get; set; } = DateTime.Now;

        // Helper for UI Logic
        public bool IsAddMode { get; set; } // Controls IsEnabled for ID/Date
        public string WindowTitle { get; set; }
        public Array TransportOptions => Enum.GetValues(typeof(BO.Transportation));


        // Constructor
        public CourierWindow(int courierId = 0)
        {
            InitializeComponent();
            DataContext = this;

            if (courierId == 0)
            {
                // ADD MODE
                IsAddMode = true;
                WindowTitle = "Add New Courier";

                // Initialize defaults
                FixedId = 0;
                FixedJoinDate = DateTime.Now;

                // Empty object to hold user inputs for other fields
                CurrentCourier = new BO.Courier();
                CurrentCourier.IsActive = true;
                CurrentCourier.Transport = BO.Transportation.Motorcycle;
            }
            else
            {
                // UPDATE MODE
                IsAddMode = false;
                WindowTitle = "Update Courier Details";

                try
                {
                    int managerId = s_bl.Admin.GetConfig().ManagerId;

                    // Load existing data
                    CurrentCourier = s_bl.Courier.Details(managerId, courierId);

                    // Copy immutable values to the fixed properties for display
                    FixedId = CurrentCourier.Id;
                    FixedJoinDate = CurrentCourier.JoinDate;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int managerId = s_bl.Admin.GetConfig().ManagerId;

                // Basic Validation
                if (string.IsNullOrWhiteSpace(CurrentCourier.FullName))
                    throw new Exception("Full Name is required.");
                if (string.IsNullOrWhiteSpace(CurrentCourier.PhoneNumber))
                    throw new Exception("Phone Number is required.");

                if (IsAddMode)
                {
                    if (FixedId <= 0) throw new Exception("ID must be positive.");

                    // CONSTRUCT the final object here because properties are 'init'
                    var newCourier = new BO.Courier
                    {
                        Id = this.FixedId,
                        JoinDate = this.FixedJoinDate,
                        // Copy mutable fields from the binding source
                        FullName = CurrentCourier.FullName,
                        Email = CurrentCourier.Email,
                        PhoneNumber = CurrentCourier.PhoneNumber,
                        Password = CurrentCourier.Password,
                        MaxDistancePreference = CurrentCourier.MaxDistancePreference,
                        Transport = CurrentCourier.Transport,
                        IsActive = CurrentCourier.IsActive,
                        // Initialize counters
                        DeliveryCountOnTime = 0,
                        DeliveryCountLate = 0
                    };

                    s_bl.Courier.Add(managerId, newCourier);
                    MessageBox.Show("Added Successfully!");
                }
                else
                {
                    // UPDATE MODE: We just pass the CurrentCourier object.
                    // The BL will ignore ID/JoinDate changes anyway, or we didn't change them.
                    s_bl.Courier.UpdateDetails(managerId, CurrentCourier);
                    MessageBox.Show("Updated Successfully!");
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation Failed: {ex.Message}", "Error");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}