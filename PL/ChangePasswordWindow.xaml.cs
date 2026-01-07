using System;
using System.Windows;
using BlApi;

namespace PL
{
    /// <summary>
    /// Interaction logic for ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        private static readonly IBl s_bl = Factory.Get();
        private readonly int _courierId;
        private readonly int _managerId;

        public ChangePasswordWindow(int courierId, int managerId)
        {
            InitializeComponent();
            _courierId = courierId;
            _managerId = managerId;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnChange_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;

            string current = pwdCurrent.Password ?? string.Empty;
            string neu = pwdNew.Password ?? string.Empty;
            string confirm = pwdConfirm.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(neu))
            {
                ShowError("Please enter both current and new password.");
                return;
            }

            if (neu != confirm)
            {
                ShowError("New password and confirmation do not match.");
                return;
            }

            try
            {
                // Validate current password via BL
                s_bl.Courier.Authenticate(_courierId, current);

                // Load courier, set new password and save using manager id (BL requires admin id for UpdateDetails)
                var courier = s_bl.Courier.Details(_managerId, _courierId);
                courier.Password = neu;
                s_bl.Courier.UpdateDetails(_managerId, courier);

                MessageBox.Show("Password changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to change password: {ex.Message}");
            }
        }

        private void ShowError(string msg)
        {
            txtError.Text = msg;
            txtError.Visibility = Visibility.Visible;
        }
    }
}