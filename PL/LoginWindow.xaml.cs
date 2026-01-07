using System;
using System.Windows;
using BlApi;
using BO;

namespace PL
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Visibility = Visibility.Collapsed;
            if (!int.TryParse(txtId.Text?.Trim(), out int id))
            {
                ShowError("Please enter a valid numeric ID.");
                return;
            }

            string password = pwdBox.Password ?? string.Empty;

            try
            {
                // 1. Manager check (Config stores manager password)
                var config = s_bl.Admin.GetConfig();
                if (id == config.ManagerId)
                {
                    if (config.ManagerPassword == password)
                    {
                        var main = new MainWindow();
                        main.Show();
                        Close();
                        return;
                    }
                    else
                    {
                        ShowError("Invalid manager password.");
                        return;
                    }
                }

                // 2. Courier authentication via BL
                var role = s_bl.Courier.Authenticate(id, password);

                if (role == JobTypes.Manager)
                {
                    var main = new MainWindow();
                    main.Show();
                    Close();
                    return;
                }

                if (role == JobTypes.Courier)
                {
                    var courierWindow = new MainCourierWindow(id);
                    courierWindow.Show();
                    Close();
                    return;
                }

                ShowError("Unknown role returned. Contact administrator.");
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtStatus.Text = message;
            txtStatus.Visibility = Visibility.Visible;
        }
    }
}
