using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BlApi;
using BO;
using PL.Courier;

namespace PL.Login
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        private static readonly IBl s_bl = Factory.Get();

        // Singleton instance tracking
        private static LoginWindow? _instance;
        private static readonly object _lock = new();

        // Bindable properties (no x:Name used in XAML for the error TextBlocks)
        private string _idErrorText = string.Empty;
        private Visibility _idErrorVisibility = Visibility.Collapsed;
        private string _passwordErrorText = string.Empty;
        private Visibility _passwordErrorVisibility = Visibility.Collapsed;

        public string IdErrorText
        {
            get => _idErrorText;
            set => SetField(ref _idErrorText, value);
        }

        public Visibility IdErrorVisibility
        {
            get => _idErrorVisibility;
            set => SetField(ref _idErrorVisibility, value);
        }

        public string PasswordErrorText
        {
            get => _passwordErrorText;
            set => SetField(ref _passwordErrorText, value);
        }

        public Visibility PasswordErrorVisibility
        {
            get => _passwordErrorVisibility;
            set => SetField(ref _passwordErrorVisibility, value);
        }

        /// <summary>
        /// Gets or creates the single LoginWindow instance.
        /// </summary>
        public static LoginWindow Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null || !_instance.IsLoaded)
                    {
                        _instance = new LoginWindow();
                    }
                    return _instance;
                }
            }
        }

        public LoginWindow()
        {
            InitializeComponent();
            ClearInlineErrors();

            // Track when window is closed to clear the instance
            Closed += (s, e) =>
            {
                lock (_lock)
                {
                    if (_instance == this)
                        _instance = null;
                }
            };
        }

        /// <summary>
        /// Shows the single LoginWindow instance. If already open, brings it to front.
        /// </summary>
        public static void ShowSingle()
        {
            var window = Instance;
            if (window.IsVisible)
            {
                window.Activate();
                window.Focus();
            }
            else
            {
                window.Show();
                window.Activate();
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // hide general status
            txtStatus.Visibility = Visibility.Collapsed;
            ClearInlineErrors();

            if (!int.TryParse(txtId.Text?.Trim(), out int id))
            {
                IdErrorText = "Please enter a valid numeric ID.";
                IdErrorVisibility = Visibility.Visible;
                return;
            }

            string password = pwdBox.Password ?? string.Empty;

            try
            {
                // Manager check (Config stores manager id/password)
                var config = s_bl.Admin.GetConfig();
                if (id == config.ManagerId)
                {
                    if (config.ManagerPassword == password)
                    {
                        OpenWindowAndClearInputs(new MainWindow());
                        return;
                    }
                    else
                    {
                        PasswordErrorText = "wrong password";
                        PasswordErrorVisibility = Visibility.Visible;
                        return;
                    }
                }

                // Courier authentication via BL
                var role = s_bl.Courier.Authenticate(id, password);

                if (role == JobTypes.Manager)
                {
                    OpenWindowAndClearInputs(new MainWindow());
                    return;
                }

                if (role == JobTypes.Courier)
                {
                    OpenWindowAndClearInputs(new PL.Courier.ForCourier.MainCourierWindow(id));
                    return;
                }

                // fallback
                txtStatus.Text = "Unknown role returned. Contact administrator.";
                txtStatus.Visibility = Visibility.Visible;
            }
            catch (DO.BlDoesNotExistException)
            {
                // ID not found
                IdErrorText = "ID doesn't exist";
                IdErrorVisibility = Visibility.Visible;
            }
            catch (DO.BlUnauthorizedAccessException)
            {
                // Wrong password
                PasswordErrorText = "wrong password";
                PasswordErrorVisibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // other failures
                txtStatus.Text = $"Login failed: {ex.Message}";
                txtStatus.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Opens the target window and ensures it appears on top.
        /// </summary>
        private void OpenWindowAndClearInputs(Window targetWindow)
        {
            // Clear inputs first (without setting focus)
            txtId.Text = string.Empty;
            pwdBox.Password = string.Empty;
            ClearInlineErrors();

            // Show and activate the new window so it appears on top
            targetWindow.Show();
            targetWindow.Activate();
            targetWindow.Focus();
        }

        private void BtnLeave_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearInputs()
        {
            txtId.Text = string.Empty;
            pwdBox.Password = string.Empty;
            ClearInlineErrors();
            txtId.Focus();
        }

        private void ClearInlineErrors()
        {
            IdErrorText = string.Empty;
            IdErrorVisibility = Visibility.Collapsed;
            PasswordErrorText = string.Empty;
            PasswordErrorVisibility = Visibility.Collapsed;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
