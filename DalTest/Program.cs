using System;
using DalApi;
using Dal;

namespace DalTest
{
    internal class Program
    {
        private static IConfig? s_dalConfig = new ConfigImplementation();        // stage 1
        private static IDelivery? s_dalDelivery = new DeliveryImplementation();  // stage 1
        private static IOrder? s_dalOrder = new OrderImplementation();           // stage 1
        private static ICourier? s_dalCourier = new CourierImplementation();     // stage 1

        // ===============================
        // ENUM DEFINITIONS
        // ===============================

        private enum MainMenu
        {
            Exit = 0,
            DeliveryMenu = 1,
            OrderMenu = 2,
            CourierMenu = 3,
            InitializeData = 4,
            ShowAllData = 5,
            ConfigMenu = 6,
            ResetDatabase = 7
        }

        private enum EntityMenu
        {
            Exit = 0,
            Create = 1,
            Read = 2,
            ReadAll = 3,
            Update = 4,
            Delete = 5,
            DeleteAll = 6
        }

        private enum ConfigMenu
        {
            Exit = 0,
            AdvanceClockMinute = 1,
            AdvanceClockHour = 2,
            ShowClock = 3,
            SetConfigValue = 4,
            ShowConfigValue = 5,
            ResetConfig = 6
        }

        // ===============================
        // MAIN ENTRY POINT
        // ===============================

        static void Main(string[] args)
        {
            try
            {

                Initialization.Do(s_dalConfig, s_dalDelivery, s_dalOrder, s_dalCourier);
                RunMainMenu();
                s_dalConfig!.Reset();
                s_dalCourier!.DeleteAll();
                s_dalOrder!.DeleteAll();
                s_dalDelivery!.DeleteAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in Main:");
                Console.WriteLine(ex);
            }
        }

        // ===============================
        // MAIN MENU
        // ===============================

        private static void RunMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== MAIN MENU ===");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Delivery Menu");
                Console.WriteLine("2. Order Menu");
                Console.WriteLine("3. Courier Menu");
                Console.WriteLine("4. Initialize Data");
                Console.WriteLine("5. Show All Data");
                Console.WriteLine("6. Configuration Menu");
                Console.WriteLine("7. Reset Database");
                Console.Write("\nChoose an option: ");

                if (!Enum.TryParse(Console.ReadLine(), out MainMenu choice))
                {
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case MainMenu.Exit:
                            return;
                        case MainMenu.DeliveryMenu:
                            RunEntityMenu("Delivery");
                            break;
                        case MainMenu.OrderMenu:
                            RunEntityMenu("Order");
                            break;
                        case MainMenu.CourierMenu:
                            RunEntityMenu("Courier");
                            break;
                        case MainMenu.InitializeData:
                            Initialization.Do();
                            break;
                        case MainMenu.ShowAllData:
                            ShowAllData();
                            break;
                        case MainMenu.ConfigMenu:
                            RunConfigMenu();
                            break;
                        case MainMenu.ResetDatabase:
                            ResetAllData();
                            break;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in main menu:");
                    Console.WriteLine(ex);
                }

                Console.WriteLine("\nPress ENTER to return to the main menu...");
                Console.ReadLine();
            }
        }

        // ===============================
        // ENTITY SUB-MENU
        // ===============================

        private static void RunEntityMenu(string entityName)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== {entityName.ToUpper()} MENU ===");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Create");
                Console.WriteLine("2. Read");
                Console.WriteLine("3. ReadAll");
                Console.WriteLine("4. Update");
                Console.WriteLine("5. Delete");
                Console.WriteLine("6. DeleteAll");
                Console.Write("\nChoose an option: ");

                if (!Enum.TryParse(Console.ReadLine(), out EntityMenu choice))
                {
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case EntityMenu.Exit:
                            return;
                        case EntityMenu.Create:
                            Console.WriteLine($"Creating new {entityName}...");
                            HandleCreate(entityName);
                            break;
                        case EntityMenu.Read:
                            Console.WriteLine($"Reading {entityName} by ID...");
                            // TODO: implement Read logic (D)
                            break;
                        case EntityMenu.ReadAll:
                            Console.WriteLine($"Showing all {entityName}s...");
                            // TODO: implement ReadAll logic
                            break;
                        case EntityMenu.Update:
                            Console.WriteLine($"Updating {entityName}...");
                            // TODO: implement Update logic
                            break;
                        case EntityMenu.Delete:
                            Console.WriteLine($"Deleting {entityName}...");
                            // TODO: implement Delete logic
                            break;
                        case EntityMenu.DeleteAll:
                            Console.WriteLine($"Deleting all {entityName}s...");
                            // TODO: implement DeleteAll logic
                            break;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in {entityName} menu:");
                    Console.WriteLine(ex);
                }

                Console.WriteLine("\nPress ENTER to return to the sub-menu...");
                Console.ReadLine();
            }
        }

        // ===============================
        // CREATE HANDLER & INPUT HELPERS
        // ===============================

        private static void HandleCreate(string entityName)
        {
            try
            {
                switch (entityName)
                {
                    case "Order":
                        {
                            int id = s_dalConfig!.NextOrderId;
                            string address = ReadString("Address of order: ");
                            double lat = ReadDouble("Latitude: ");
                            double lon = ReadDouble("Longitude: ");
                            string customerName = ReadString("Customer name: ");
                            string customerPhone = ReadString("Customer phone: ");
                            DateTime createdAt = s_dalConfig.Clock;
                            bool fragile = ReadBool("Fragile (y/n): ");
                            int weight = ReadInt("Weight (integer): ");
                            int volume = ReadInt("Volume (integer): ");
                            string additional = ReadString("Additional details (empty = none): ", allowEmpty: true);
                            if (string.IsNullOrWhiteSpace(additional)) additional = null;
                            string description = ReadString("Description (empty = none): ", allowEmpty: true);
                            if (string.IsNullOrWhiteSpace(description)) description = null;

                            var order = new Order(id, address, lat, lon, customerName, customerPhone, createdAt, fragile, weight, volume, additional, description);
                            s_dalOrder!.Create(order);
                            Console.WriteLine($"Order created with Id: {id}");
                            break;
                        }
                    case "Courier":
                        {
                            // Id left as 0 to let DAL implementation assign if it does
                            int id = 0;
                            bool active = ReadBool("Active (y/n): ");
                            double? maxDistance = ReadDouble("Max delivery distance (km) (empty = none): ", allowEmpty: true);
                            DateTime deliveryTime = s_dalConfig!.Clock;
                            var orderType = ReadEnumOptional<OrderType>("Order type (Car/Motorcycle/Bike/Walking): ");
                            string name = ReadString("Name: ");
                            string phone = ReadString("Phone: ");
                            string email = ReadString("Email: ");
                            string password = ReadString("Password: ");

                            var courier = new Courier(id, active, maxDistance, deliveryTime, orderType, name, phone, email, password);
                            s_dalCourier!.Create(courier);
                            Console.WriteLine("Courier created.");
                            break;
                        }
                    case "Delivery":
                        {
                            int id = s_dalConfig!.NextDeliveryId;
                            int orderId = ReadInt("Order Id: ");
                            int courierId = ReadInt("Courier Id: ");
                            var orderType = ReadEnumOptional<OrderType>("Order type (Car/Motorcycle/Bike/Walking): ");
                            DateTime start = s_dalConfig.Clock;
                            double? actualDistance = ReadDouble("Actual distance (km) (empty = none): ", allowEmpty: true);
                            var endOfOrder = ReadEnumOptional<EndOfOrder>("End of order (Delivered/Cancelled/Failed) (empty = none): ");
                            DateTime? timeOfDelivery = null;
                            string tod = ReadString("Time of delivery (yyyy-MM-dd HH:mm) (empty = none): ", allowEmpty: true);
                            if (!string.IsNullOrWhiteSpace(tod) && DateTime.TryParse(tod, out var parsed))
                                timeOfDelivery = parsed;

                            var delivery = new Delivery(id, orderId, courierId, orderType, start, actualDistance, endOfOrder, timeOfDelivery);
                            s_dalDelivery!.Create(delivery);
                            Console.WriteLine($"Delivery created with Id: {id}");
                            break;
                        }
                    default:
                        Console.WriteLine("Create not supported for this entity.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating entity:");
                Console.WriteLine(ex.Message);
            }
        }

        private static string ReadString(string prompt, bool allowEmpty = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (s is null) s = "";
                if (!allowEmpty && string.IsNullOrWhiteSpace(s))
                {
                    Console.WriteLine("Value required.");
                    continue;
                }
                return s;
            }
        }

        private static int ReadInt(string prompt, int? defaultIfEmpty = null)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s) && defaultIfEmpty.HasValue) return defaultIfEmpty.Value;
                if (int.TryParse(s, out var v)) return v;
                Console.WriteLine("Invalid integer.");
            }
        }

        private static double ReadDouble(string prompt, bool allowEmpty = false, double? defaultIfEmpty = null)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                {
                    if (allowEmpty) return defaultIfEmpty ?? double.NaN;
                    if (defaultIfEmpty.HasValue) return defaultIfEmpty.Value;
                    Console.WriteLine("Value required.");
                    continue;
                }
                if (double.TryParse(s, out var v)) return v;
                Console.WriteLine("Invalid number.");
            }
        }

        // Overload returning nullable double when allowEmpty true
        private static double? ReadDouble(string prompt, bool allowEmpty)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                {
                    if (allowEmpty) return null;
                    Console.WriteLine("Value required.");
                    continue;
                }
                if (double.TryParse(s, out var v)) return v;
                Console.WriteLine("Invalid number.");
            }
        }

        private static bool ReadBool(string prompt, bool? defaultIfEmpty = null)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s) && defaultIfEmpty.HasValue) return defaultIfEmpty.Value;
                if (string.IsNullOrWhiteSpace(s)) { Console.WriteLine("Value required."); continue; }
                s = s.Trim().ToLowerInvariant();
                if (s == "y" || s == "yes" || s == "true" || s == "1") return true;
                if (s == "n" || s == "no" || s == "false" || s == "0") return false;
                Console.WriteLine("Invalid boolean. Enter y/n.");
            }
        }

        private static T ReadEnumOptional<T>(string prompt) where T : Enum
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                {
                    // return default enum value if not provided
                    return default!;
                }
                if (Enum.TryParse(typeof(T), s, true, out var val))
                {
                    return (T)val!;
                }
                Console.WriteLine($"Invalid value. Valid values: {string.Join(", ", Enum.GetNames(typeof(T)))}");
            }
        }

        // ===============================
        // CONFIGURATION SUB-MENU
        // ===============================

        private static void RunConfigMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== CONFIGURATION MENU ===");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Advance system clock by 1 minute");
                Console.WriteLine("2. Advance system clock by 1 hour");
                Console.WriteLine("3. Show current clock value");
                Console.WriteLine("4. Set configuration value");
                Console.WriteLine("5. Show configuration value");
                Console.WriteLine("6. Reset all configuration values");
                Console.Write("\nChoose an option: ");

                if (!Enum.TryParse(Console.ReadLine(), out ConfigMenu choice))
                {
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case ConfigMenu.Exit:
                            return;
                        case ConfigMenu.AdvanceClockMinute:
                            s_dalConfig!.Clock = s_dalConfig.Clock.AddMinutes(1);
                            Console.WriteLine("Clock advanced by 1 minute.");
                            break;
                        case ConfigMenu.AdvanceClockHour:
                            s_dalConfig!.Clock = s_dalConfig.Clock.AddHours(1);
                            Console.WriteLine("Clock advanced by 1 hour.");
                            break;
                        case ConfigMenu.ShowClock:
                            Console.WriteLine($"Current Clock: {s_dalConfig!.Clock}");
                            break;
                        case ConfigMenu.SetConfigValue:
                            Console.WriteLine("TODO: Set specific config value");
                            break;
                        case ConfigMenu.ShowConfigValue:
                            Console.WriteLine("TODO: Show specific config value");
                            break;
                        case ConfigMenu.ResetConfig:
                            s_dalConfig!.Reset();
                            Console.WriteLine("Configuration reset successfully.");
                            break;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Config menu:");
                    Console.WriteLine(ex);
                }

                Console.WriteLine("\nPress ENTER to return to the configuration menu...");
                Console.ReadLine();
            }
        }

        // ===============================
        // PLACEHOLDER METHODS
        // ===============================

        private static void ShowAllData()
        {
            Console.WriteLine("TODO: Display all entities (orders, couriers, deliveries, config...)");
        }

        private static void ResetAllData()
        {
            Console.WriteLine("TODO: Clear all tables and reset config values");
        }
    }
}
