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
                // When using in-memory DAL (list), seed it with test data
                var dalType = DalApi.Factory.Get.GetType().Name.ToLowerInvariant();
                if (dalType.Contains("list"))
                {
                    // Initialize DB via BL (this calls DalTest.Initialization.Do which creates test data)
                    BlApi.Factory.Get().Admin.InitializeDB();

                    // Get the DAL instance to configure manager credentials
                    var dal = DalApi.Factory.Get;
                    var couriers = dal.Courier.ReadAll().ToList();

                    if (couriers.Count == 0)
                    {
                        MessageBox.Show("Warning: In-memory DAL initialized but no couriers were created.\n" +
                                        "You won't be able to log in. Use 'Initialize DB' from the manager menu.",
                                        "DAL Initialization", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Set manager credentials using first courier's ID and the default password from initialization
                        dal.Config.ManagerId = 10003;
                        dal.Config.ManagerPassword = "1234";

                        // Show diagnostic info (you can comment this out after confirming it works)
                        MessageBox.Show($"In-memory DAL initialized successfully!\n\n" +
                                        $"Couriers created: {couriers.Count}\n" +
                                        $"Manager ID: {dal.Config.ManagerId}\n" +
                                        $"Manager Password: 1234\n\n" +
                                        $"You can also log in as any courier ID with password '1234'",
                                        "DAL Initialization", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize in-memory DAL:\n\n{ex.GetType().Name}: {ex.Message}\n\nStack:\n{ex.StackTrace}",
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
