using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BlApi;
using BO;

namespace PL.AvailableOrders
{
    /// <summary>
    /// Interaction logic for OrderDetailsWindow.xaml
    /// </summary>
    public partial class OrderDetailsWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _requesterId;
        private readonly int _orderId;
        private readonly bool _isManager;

        public OrderDetailsWindow()
        {
            InitializeComponent();
        }

        // Construct with BO.Order (legacy support if used elsewhere)
        public OrderDetailsWindow(BO.Order order) : this()
        {
            DataContext = order;
            BtnAccept.Visibility = Visibility.Collapsed; // Default view mode
        }

        public OrderDetailsWindow(int requesterId, int orderId, bool isManager = false)
        {
            InitializeComponent();
            _requesterId = requesterId;
            _orderId = orderId;
            _isManager = isManager;

            // Logic: Managers see only Close. Couriers see Close + Accept.
            if (_isManager)
            {
                BtnAccept.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnAccept.Visibility = Visibility.Visible;
            }

            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            try
            {
                // To View details, we need Admin permissions in BL. 
                // Since a Courier is not an Admin, we use the System Admin ID to fetch the details for display.
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
                // To Accept (Choose) an order, we must use the Courier's ID (_requesterId).
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
    }
}