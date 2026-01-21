using System;
using System.Linq;
using System.Windows;
using BlApi;
using DalApi;

namespace PL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Get the BL instance - PL should ONLY communicate with BL
                var bl = BlApi.Factory.Get();

                // Initialize DB via BL (this calls DalTest.Initialization.Do which creates test data)
                // This will work regardless of which DAL implementation is configured
                bl.Admin.InitializeDB();

                // Get config through BL to show diagnostic info
                var config = bl.Admin.GetConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize database:\n\n{ex.GetType().Name}: {ex.Message}",
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PL.Login.LoginWindow.ShowSingle();
        }
    }
}
