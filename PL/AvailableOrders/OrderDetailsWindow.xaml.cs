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
using BO;

namespace PL.AvailableOrders
{
    /// <summary>
    /// Interaction logic for OrderDetailsWindow.xaml
    /// </summary>
    public partial class OrderDetailsWindow : Window
    {
        public OrderDetailsWindow()
        {
            InitializeComponent();
        }

        // Construct with BO.Order and bind its data for display
        public OrderDetailsWindow(BO.Order order) : this()
        {
            DataContext = order;
        }

        // Close handler (wired in XAML)
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // OK handler (placeholder - closes by default)
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // You can add confirmation logic here if needed
            Close();
        }
    }
}
