using System;
using DalApi;
using Dal;
using DO;

namespace DalTest
{
    internal class Program
    {
        //static readonly IDal s_dal = new DalList(); //stage 2
        //static readonly IDal s_dal = new DalXml(); //stage 3
        static readonly IDal s_dal = Factory.Get; //stage 4

        // ===============================
        // ENUM DEFINITIONS
        // ===============================

        private enum MainMenu
        {
            Exit,
            DeliveryMenu,
            OrderMenu,
            CourierMenu,
            InitializeData,
            ShowAllData,
            ConfigMenu,
            ResetDatabase
        }
        private enum EntityMenu
        {
            Exit,
            Create,
            Read,
            ReadAll,
            Update,
            Delete,
            DeleteAll
        }
        private enum ConfigMenu
        {
            Exit,
            AdvanceClockMinute,
            AdvanceClockHour,
            ShowClock,
            SetConfigValue,
            ShowConfigValue,
            ResetConfig
        }
        private enum SetConfigMenu
        {
            Exit,
            CompanyAddress,
            CompanyLatitude,
            CompanyLongitude,
            MaxDeliveryDistance,
            AverageCarSpeed,
            AverageMotorcyleSpeed,
            AverageBicycleSpeed,
            AverageWalkingSpeed,
            MaxDeliveryTime,
            RiskRange,
            InactiveTime,
            ManagerId,
            ManagerPassword,
        }

        // ===============================
        // MAIN ENTRY POINT
        // ===============================

        static void Main(string[] args){
            try{

                // Initialization.Do(s_dalConfig, s_dalDelivery, s_dalOrder, s_dalCourier); //Stage 1
                //Initialization.Do(s_dal);
                Initialization.Do(); //stage 4
                RunMainMenu();
                //s_dalConfig!.Reset();
                //s_dalCourier!.DeleteAll();
                //s_dalOrder!.DeleteAll();
                //s_dalDelivery!.DeleteAll();
            }
            catch (Exception ex){
                Console.WriteLine("Unexpected error in Main:");
                Console.WriteLine(ex);
            }
        }

        // ===============================
        // MAIN MENU
        // ===============================

        private static void RunMainMenu(){
            while (true){
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

                if (!Enum.TryParse(Console.ReadLine(), out MainMenu choice)){
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                try{
                    switch (choice){
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
                            Initialization.DataInit();
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
                catch (Exception ex){
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
                            CreateEntity(entityName);
                            break;
                        case EntityMenu.Read:
                            ReadEntity(entityName);
                            break;
                        case EntityMenu.ReadAll:
                            Console.WriteLine($"Showing all {entityName}s...");
                            ReadAllEntities(entityName);
                            break;
                        case EntityMenu.Update:
                            Console.WriteLine($"Updating {entityName}...");
                            UpdateEntity(entityName);
                            break;
                        case EntityMenu.Delete:
                            Console.WriteLine($"Deleting {entityName}...");
                            DeleteEntity(entityName);
                            break;
                        case EntityMenu.DeleteAll:
                            Console.WriteLine($"Deleting all {entityName}s...");
                            DeleteAllEntities(entityName);
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

        private static void CreateEntity(string entityName)
        {
            try
            {
                switch (entityName)
                {
                    case "Order":
                        {
                            int id = s_dal.Config.NextOrderId;
                            string address = ReadString("Address of order: ");
                            double lat = ReadDouble("Latitude: ");
                            double lon = ReadDouble("Longitude: ");
                            string customerName = ReadString("Customer name: ");
                            string customerPhone = ReadString("Customer phone: ");
                            DateTime createdAt = s_dal.Config.Clock;
                            bool fragile = ReadBool("Fragile (y/n): ");
                            OrderType orderType = ReadEnumOptional<OrderType>("Order type (Car/Motorcycle/Bike/Walking): ");
                            int weight = ReadInt("Weight (integer): ");
                            int volume = ReadInt("Volume (integer): ");
                            string additional = ReadString("Additional details (empty = none): ", allowEmpty: true);
                            if (string.IsNullOrWhiteSpace(additional)) additional = null;
                            string description = ReadString("Description (empty = none): ", allowEmpty: true);
                            if (string.IsNullOrWhiteSpace(description)) description = null;

                            var order = new Order(id, address, lat, lon, customerName, customerPhone, createdAt, fragile, weight, volume, orderType, additional, description);
                            s_dal.Order!.Create(order);
                            Console.WriteLine($"Order created with Id: {id}");
                            break;
                        }
                    case "Courier":
                        {
                            // Id left as 0 to let DAL implementation assign if it does
                            int id = ReadInt("Enter the courier's ID:");
                            bool active = ReadBool("Active (y/n): ");
                            double? maxDistance = ReadDouble("Max delivery distance (km) (empty = none): ", allowEmpty: true);
                            DateTime deliveryTime = s_dal.Config!.Clock;
                            var orderType = ReadEnumOptional<OrderType>("Order type (Car/Motorcycle/Bike/Walking): ");
                            string name = ReadString("Name: ");
                            string phone = ReadString("Phone: ");
                            string email = ReadString("Email: ");
                            string password = ReadString("Password: ");

                            var courier = new Courier(id, active, maxDistance, deliveryTime, orderType, name, phone, email, password);
                            s_dal.Courier!.Create(courier);
                            Console.WriteLine("Courier created.");
                            break;
                        }
                    case "Delivery":
                        {
                            int id = s_dal.Config!.NextDeliveryId;
                            int orderId = ReadInt("Order Id: ");
                            int courierId = ReadInt("Courier Id: ");
                            var orderType = ReadEnumOptional<OrderType>("Order type (Car/Motorcycle/Bike/Walking): ");
                            DateTime start = s_dal.Config.Clock;
                            double? actualDistance = ReadDouble("Actual distance (km) (empty = none): ", allowEmpty: true);
                            var endOfOrder = ReadEnumOptional<EndOfOrder>("End of order (Delivered/Cancelled/Failed) (empty = none): ");
                            DateTime? timeOfDelivery = null;
                            string tod = ReadString("Time of delivery (yyyy-MM-dd HH:mm) (empty = none): ", allowEmpty: true);
                            if (!string.IsNullOrWhiteSpace(tod) && DateTime.TryParse(tod, out var parsed))
                                timeOfDelivery = parsed;

                            var delivery = new Delivery(id, orderId, courierId, orderType, start, actualDistance, endOfOrder, timeOfDelivery);
                            s_dal.Delivery!.Create(delivery);
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

        private static void ReadEntity(string entityName)
        {
            Console.WriteLine($"Reading {entityName} by ID...");
            {
                int id = ReadInt("Enter ID: ");
                try
                {
                    switch (entityName)
                    {
                        case "Order":
                            var order = s_dal.Order!.Read(id);
                            if (order is null)
                            {
                                Console.WriteLine($"Order with Id {id} not found.");
                            }
                            else
                            {
                                PrintOrder(order);
                            }
                            break;
                        case "Courier":
                            var courier = s_dal.Courier!.Read(id);
                            if (courier is null)
                            {
                                Console.WriteLine($"Courier with Id {id} not found.");
                            }
                            else
                            {
                                PrintCourier(courier);
                            }
                            break;
                        case "Delivery":
                            var delivery = s_dal.Delivery!.Read(id);
                            if (delivery is null)
                            {
                                Console.WriteLine($"Delivery with Id {id} not found.");
                            }
                            else
                            {
                                PrintDelivery(delivery);
                            }
                            break;
                        default:
                            Console.WriteLine("Read not supported for this entity.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while reading entity:");
                    Console.WriteLine(ex.Message);
                }
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
                if (string.IsNullOrWhiteSpace(s) && defaultIfEmpty.HasValue)
                    return defaultIfEmpty.Value;
                if (int.TryParse(s, out var v)) 
                    return v;
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
                    if (allowEmpty) 
                        return defaultIfEmpty ?? double.NaN;
                    if (defaultIfEmpty.HasValue)
                        return defaultIfEmpty.Value;
                    Console.WriteLine("Value required.");
                    continue;
                }
                if (double.TryParse(s, out var v))
                    return v;
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
                    if (allowEmpty)
                        return null;
                    Console.WriteLine("Value required.");
                    continue;
                }
                if (double.TryParse(s, out var v))
                    return v;
                Console.WriteLine("Invalid number.");
            }
        }

        private static bool ReadBool(string prompt, bool? defaultIfEmpty = null)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s) && defaultIfEmpty.HasValue) 
                    return defaultIfEmpty.Value;
                if (string.IsNullOrWhiteSpace(s)) { Console.WriteLine("Value required.");
                    continue; }
                s = s.Trim().ToLowerInvariant();
                if (s == "y" || s == "yes" || s == "true" || s == "1") 
                    return true;
                if (s == "n" || s == "no" || s == "false" || s == "0")
                    return false;
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
            Console.WriteLine("Enter Manager Password");
            string PW;
            do
            {
                PW = Console.ReadLine();
                if (PW != s_dal.Config!.ManagerPassword)
                {
                        Console.WriteLine("Incorrect Password, try again");
                }

            } while (PW != s_dal.Config!.ManagerPassword);
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
                            s_dal.Config!.Clock = s_dal.Config.Clock.AddMinutes(1);
                            Console.WriteLine("Clock advanced by 1 minute.");
                            break;
                        case ConfigMenu.AdvanceClockHour:
                            s_dal.Config!.Clock = s_dal.Config.Clock.AddHours(1);
                            Console.WriteLine("Clock advanced by 1 hour.");
                            break;
                        case ConfigMenu.ShowClock:
                            Console.WriteLine($"Current Clock: {s_dal.Config!.Clock}");
                            break;
                        case ConfigMenu.SetConfigValue:
                            //Console.WriteLine("TODO: Set specific config value");
                            Console.WriteLine("Select variable to set:");
                            Console.WriteLine("0. Exit");
                            Console.WriteLine("1. Company Address");
                            Console.WriteLine("2. Company Latitude");
                            Console.WriteLine("3. Company Longitude");
                            Console.WriteLine("4. Max Delivery Distance");
                            Console.WriteLine("5. Average Car Speed");
                            Console.WriteLine("6. Average Motorcyle Speed ");
                            Console.WriteLine("7. Average Bike Speed");
                            Console.WriteLine("8. Average Walking Speed");
                            Console.WriteLine("9. Max Delivery Time");
                            Console.WriteLine("10. Risk Range");
                            Console.WriteLine("11. Inactive Time");
                            Console.WriteLine("12. Manager Id");
                            Console.WriteLine("13. Manager Password");
                            if (!Enum.TryParse(Console.ReadLine(), out SetConfigMenu SetConfigChoice))
                            {
                                Console.WriteLine("Invalid input.");
                                continue;
                            }
                            switch (SetConfigChoice)
                            {
                                case SetConfigMenu.Exit:
                                    break;
                                case SetConfigMenu.CompanyAddress:
                                    Console.WriteLine("Enter new Company Address:");
                                    string? newAddress = Console.ReadLine();
                                    break;
                                case SetConfigMenu.CompanyLatitude:
                                    break;
                                case SetConfigMenu.CompanyLongitude:
                                    break;
                                case SetConfigMenu.MaxDeliveryDistance:
                                    break;
                                case SetConfigMenu.AverageCarSpeed:
                                    break;
                                case SetConfigMenu.AverageMotorcyleSpeed:
                                    break;
                                case SetConfigMenu.AverageBicycleSpeed:
                                    break;
                                case SetConfigMenu.AverageWalkingSpeed:
                                    break;
                                case SetConfigMenu.MaxDeliveryTime:
                                    break;
                                case SetConfigMenu.RiskRange:
                                    break;
                                case SetConfigMenu.InactiveTime:
                                    break;
                                case SetConfigMenu.ManagerId:
                                    break;
                                case SetConfigMenu.ManagerPassword:
                                    break;
                                default:
                                    break;
                            }







                            break;
                        case ConfigMenu.ShowConfigValue:
                            Console.WriteLine("Showing all configuration values: ");
                            Console.WriteLine();
                            break;
                        case ConfigMenu.ResetConfig:
                            s_dal.Config!.Reset();
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

        private static void ShowAllData(){
            Console.WriteLine("Showing all data:");
            Console.WriteLine("Couriers:");
            List <Courier> listCourier = (List<Courier>)s_dal.Courier.ReadAll().ToList();
            foreach (var item in listCourier){
                PrintCourier(item);
                Console.WriteLine();
            }

            Console.WriteLine("Orders:");
            List<Order> listOrder = (List<Order>)s_dal.Order.ReadAll().ToList();
            foreach (var item in listOrder){
                PrintOrder(item);
                Console.WriteLine();
            }
            Console.WriteLine("Deliveries:");
            List<Delivery> listDelivery = (List<Delivery>)s_dal.Delivery.ReadAll().ToList();
            foreach (var item in listDelivery){
                PrintDelivery(item);
                Console.WriteLine();
            }
        }

        private static void ResetAllData(){
            Console.WriteLine("Are you sure you want to reset all the data? (y/n)");
            string? res = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(res) || !(res.Trim().ToLowerInvariant() == "y" || res.Trim().ToLowerInvariant() == "yes"))
            {
                Console.WriteLine("Aborted.");
                return;
            }
            s_dal.Delivery!.DeleteAll();
            s_dal.Order!.DeleteAll();
            s_dal.Courier!.DeleteAll();
            s_dal.Config!.Reset();
            Console.WriteLine("All data reset successfully.");
        }

        // ===============================
        // PRINT HELPERS
        // ===============================

        private static void PrintOrder(Order o)
        {
            Console.WriteLine("=== Order ===");
            Console.WriteLine($"Id: {o.Id}");
            Console.WriteLine($"AddressOfOrder: {o.AdderssOfOrder}");
            Console.WriteLine($"Latitude: {o.Latitude}");
            Console.WriteLine($"Longtitude: {o.Longtitude}");
            Console.WriteLine($"CustomerName: {o.CustomerName}");
            Console.WriteLine($"CustomerPhone: {o.CustomerPhone}");
            Console.WriteLine($"CreatedAt: {o.CreatedAt}");
            Console.WriteLine($"Fragile: {o.Fragile}");
            Console.WriteLine($"Weight: {o.Weight}");
            Console.WriteLine($"Volume: {o.Volume}");
            Console.WriteLine($"AdditionalDetails: {(string.IsNullOrWhiteSpace(o.AdditionalDetails) ? "(none)" : o.AdditionalDetails)}");
            Console.WriteLine($"Description: {(string.IsNullOrWhiteSpace(o.Description) ? "(none)" : o.Description)}");
        }

        private static void PrintCourier(Courier c)
        {
            Console.WriteLine("=== Courier ===");
            Console.WriteLine($"Id: {c.Id}");
            Console.WriteLine($"Active: {c.Active}");
            Console.WriteLine($"MaxAirDeliveryDistance: {(c.MaxDeliveryDistance.HasValue ? c.MaxDeliveryDistance.Value.ToString() : "(none)")}");
            Console.WriteLine($"Joined the company at: {c.JoinDate}");
            Console.WriteLine($"OrderType: {c.OrderType}");
            Console.WriteLine($"Name: {c.Name}");
            Console.WriteLine($"Phone: {c.Phone}");
            Console.WriteLine($"Email: {c.Email}");
            Console.WriteLine($"Password: {(string.IsNullOrEmpty(c.Password) ? "(none)" : c.Password)}");
        }

        private static void PrintDelivery(Delivery d)
        {
            Console.WriteLine("=== Delivery ===");
            Console.WriteLine($"Id: {d.Id}");
            Console.WriteLine($"OrderId: {d.OrderId}");
            Console.WriteLine($"CourierId: {d.CourierId}");
            Console.WriteLine($"OrderType: {d.OrderType}");
            Console.WriteLine($"StartOfDelivery: {d.StartOfDelivery}");
            Console.WriteLine($"ActualDistance: {(d.ActualDistance.HasValue ? d.ActualDistance.Value.ToString() : "(none)")}");
            Console.WriteLine($"EndOfOrder: {(d.EndOfOrder.HasValue ? d.EndOfOrder.Value.ToString() : "(none)")}");
            Console.WriteLine($"TimeOfDelivery: {(d.TimeOfDelivery.HasValue ? d.TimeOfDelivery.Value.ToString() : "(none)")}");
        }
        private static void ReadAllEntities(string entityName)
        {
            try
            {
                switch (entityName)
                {
                    case "Order":
                        var orders = s_dal.Order!.ReadAll();
                        if (orders == null || orders.Count() == 0) Console.WriteLine("No orders found.");
                        else foreach (var o in orders) { PrintOrder(o); Console.WriteLine(); }
                        break;
                    case "Courier":
                        var couriers = s_dal.Courier!.ReadAll();
                        if (couriers == null || couriers.Count() == 0) Console.WriteLine("No couriers found.");
                        else foreach (var c in couriers) { PrintCourier(c); Console.WriteLine(); }
                        break;
                    case "Delivery":
                        var deliveries = s_dal.Delivery!.ReadAll();
                        if (deliveries == null || deliveries.Count() == 0) Console.WriteLine("No deliveries found.");
                        else foreach (var d in deliveries) { PrintDelivery(d); Console.WriteLine(); }
                        break;
                    default:
                        Console.WriteLine("ReadAll not supported for this entity.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while reading all entities:");
                Console.WriteLine(ex.Message);
            }
        }
        private static void UpdateEntity(string entityName)
        {
            Console.WriteLine($"Updating {entityName} by ID...");
            int id = ReadInt("Enter ID: ");
            try
            {
                switch (entityName)
                {
                    case "Order":
                        var existingOrder = s_dal.Order!.Read(id);
                        if (existingOrder is null) { Console.WriteLine($"Order with Id {id} not found."); return; }
                        PrintOrder(existingOrder);
                        Console.WriteLine("--- Enter new values (empty = keep current) ---");

                        string address = ReadString($"Address [{existingOrder.AdderssOfOrder}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(address)) address = existingOrder.AdderssOfOrder;

                        double latitude = ReadNullableDouble($"Latitude [{existingOrder.Latitude}]: ", existingOrder.Latitude);
                        double longitude = ReadNullableDouble($"Longitude [{existingOrder.Longtitude}]: ", existingOrder.Longtitude);

                        string customerName = ReadString($"Customer name [{existingOrder.CustomerName}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(customerName)) customerName = existingOrder.CustomerName;

                        string customerPhone = ReadString($"Customer phone [{existingOrder.CustomerPhone}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(customerPhone)) customerPhone = existingOrder.CustomerPhone;

                        bool fragile = ReadNullableBool($"Fragile (y/n) [{(existingOrder.Fragile ? "y" : "n")}]: ", existingOrder.Fragile);
                        int weight = ReadNullableInt($"Weight [{existingOrder.Weight}]: ", existingOrder.Weight);
                        int volume = ReadNullableInt($"Volume [{existingOrder.Volume}]: ", existingOrder.Volume);
                        OrderType orderType = ReadEnumKeepCurrent<OrderType>($"Order type ({string.Join("/", Enum.GetNames(typeof(OrderType)))}) [{existingOrder.OrderType}]: ", existingOrder.OrderType);
                        string additional = ReadString($"Additional details [{(string.IsNullOrWhiteSpace(existingOrder.AdditionalDetails) ? "(none)" : existingOrder.AdditionalDetails)}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(additional)) additional = existingOrder.AdditionalDetails;

                        string description = ReadString($"Description [{(string.IsNullOrWhiteSpace(existingOrder.Description) ? "(none)" : existingOrder.Description)}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(description)) description = existingOrder.Description;

                        var updatedOrder = new Order(id, address, latitude, longitude, customerName, customerPhone, existingOrder.CreatedAt, fragile, weight, volume,orderType, additional, description);
                        s_dal.Order.Update(updatedOrder);
                        Console.WriteLine("Order updated.");
                        break;

                    case "Courier":
                        var existingCourier = s_dal.Courier!.Read(id);
                        if (existingCourier is null) { Console.WriteLine($"Courier with Id {id} not found."); return; }
                        PrintCourier(existingCourier);
                        Console.WriteLine("--- Enter new values (empty = keep current) ---");

                        bool active = ReadNullableBool($"Active (y/n) [{(existingCourier.Active ? "y" : "n")}]: ", existingCourier.Active);
                        double? maxDistance = ReadNullableDoubleNullable($"Max delivery distance (km) [{(existingCourier.MaxDeliveryDistance.HasValue ? existingCourier.MaxDeliveryDistance.Value.ToString() : "(none)")}]: ", existingCourier.MaxDeliveryDistance);
                        var orderTypeC = ReadEnumKeepCurrent<OrderType>($"Order type ({string.Join("/", Enum.GetNames(typeof(OrderType)))}) [{existingCourier.OrderType}]: ", existingCourier.OrderType);
                        string name = ReadString($"Name [{existingCourier.Name}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(name)) name = existingCourier.Name;
                        string phone = ReadString($"Phone [{existingCourier.Phone}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(phone)) phone = existingCourier.Phone;
                        string email = ReadString($"Email [{existingCourier.Email}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(email)) email = existingCourier.Email;
                        string password = ReadString($"Password [{(string.IsNullOrEmpty(existingCourier.Password) ? "(none)" : existingCourier.Password)}]: ", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(password)) password = existingCourier.Password;

                        var updatedCourier = new Courier(id, active, maxDistance, existingCourier.JoinDate, orderTypeC, name, phone, email, password);
                        s_dal.Courier.Update(updatedCourier);
                        Console.WriteLine("Courier updated.");
                        break;

                    case "Delivery":
                        var existingDelivery = s_dal.Delivery!.Read(id);
                        if (existingDelivery is null) { Console.WriteLine($"Delivery with Id {id} not found."); return; }
                        PrintDelivery(existingDelivery);
                        Console.WriteLine("--- Enter new values (empty = keep current) ---");

                        int orderId = ReadNullableInt($"Order Id [{existingDelivery.OrderId}]: ", existingDelivery.OrderId);
                        int courierId = ReadNullableInt($"Courier Id [{existingDelivery.CourierId}]: ", existingDelivery.CourierId);
                        var delOrderType = ReadEnumKeepCurrent<OrderType>($"Order type ({string.Join("/", Enum.GetNames(typeof(OrderType)))}) [{existingDelivery.OrderType}]: ", existingDelivery.OrderType);
                        double? actualDistance = ReadNullableDoubleNullable($"Actual distance (km) [{(existingDelivery.ActualDistance.HasValue ? existingDelivery.ActualDistance.Value.ToString() : "(none)")}]: ", existingDelivery.ActualDistance);
                        var endOfOrder = ReadEnumNullableKeepCurrent<EndOfOrder>($"End of order ({string.Join("/", Enum.GetNames(typeof(EndOfOrder)))}) [{(existingDelivery.EndOfOrder.HasValue ? existingDelivery.EndOfOrder.Value.ToString() : "(none)")}]: ", existingDelivery.EndOfOrder);
                        DateTime? timeOfDelivery = ReadNullableDateTime($"Time of delivery (yyyy-MM-dd HH:mm) [{(existingDelivery.TimeOfDelivery.HasValue ? existingDelivery.TimeOfDelivery.Value.ToString("yyyy-MM-dd HH:mm") : "(none)")}]: ", existingDelivery.TimeOfDelivery);

                        var updatedDelivery = new Delivery(id, orderId, courierId, delOrderType, existingDelivery.StartOfDelivery, actualDistance, endOfOrder, timeOfDelivery);
                        s_dal.Delivery.Update(updatedDelivery);
                        Console.WriteLine("Delivery updated.");
                        break;

                    default:
                        Console.WriteLine("Update not supported for this entity.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating entity:");
                Console.WriteLine(ex.Message);
            }

            // Local helper functions for parsing/optional reads
            int ReadNullableInt(string prompt, int current)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (int.TryParse(s, out var v)) return v;
                    Console.WriteLine("Invalid integer.");
                }
            }

            double ReadNullableDouble(string prompt, double current)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (double.TryParse(s, out var v)) return v;
                    Console.WriteLine("Invalid number.");
                }
            }

            double? ReadNullableDoubleNullable(string prompt, double? current)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (double.TryParse(s, out var v)) return v;
                    Console.WriteLine("Invalid number.");
                }
            }

            bool ReadNullableBool(string prompt, bool current)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    s = s.Trim().ToLowerInvariant();
                    if (s == "y" || s == "yes" || s == "true" || s == "1") return true;
                    if (s == "n" || s == "no" || s == "false" || s == "0") return false;
                    Console.WriteLine("Invalid boolean. Enter y/n.");
                }
            }

            DateTime? ReadNullableDateTime(string prompt, DateTime? current)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (DateTime.TryParse(s, out var dt)) return dt;
                    Console.WriteLine("Invalid date/time. Use yyyy-MM-dd HH:mm or other valid format.");
                }
            }

            T ReadEnumKeepCurrent<T>(string prompt, T current) where T : Enum
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (Enum.TryParse(typeof(T), s, true, out var val)) return (T)val!;
                    Console.WriteLine($"Invalid value. Valid values: {string.Join(", ", Enum.GetNames(typeof(T)))}");
                }
            }

            T? ReadEnumNullableKeepCurrent<T>(string prompt, T? current) where T : struct, Enum
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s)) return current;
                    if (Enum.TryParse(typeof(T), s, true, out var val)) return (T)val!;
                    Console.WriteLine($"Invalid value. Valid values: {string.Join(", ", Enum.GetNames(typeof(T)))}");
                }
            }
        }
        private static void DeleteEntity(string entityName)
        {
            int id = ReadInt("Enter ID to delete: ");
            try
            {
                switch (entityName)
                {
                    case "Order":
                        s_dal.Order!.Delete(id);
                        Console.WriteLine("Order deleted");
                        break;
                    case "Courier":
                        s_dal.Courier!.Delete(id);
                        Console.WriteLine("Courier deleted");
                        break;
                    case "Delivery":
                        s_dal.Delivery!.Delete(id);
                        Console.WriteLine("Delivery deleted");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting entity:");
                Console.WriteLine(ex.Message);
            }
        }
        private static void DeleteAllEntities(string entityName)
        {
            Console.Write("Are you sure you want to permanently delete ALL records for this entity? (y/n): ");
            string? res = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(res) || !(res.Trim().ToLowerInvariant() == "y" || res.Trim().ToLowerInvariant() == "yes"))
            {
                Console.WriteLine("Aborted.");
                return;
            }

            try
            {
                switch (entityName)
                {
                    case "Order":
                        s_dal.Order!.DeleteAll();
                        Console.WriteLine("All orders deleted.");
                        break;
                    case "Courier":
                        s_dal.Courier!.DeleteAll();
                        Console.WriteLine("All couriers deleted.");
                        break;
                    case "Delivery":
                        s_dal.Delivery!.DeleteAll();
                        Console.WriteLine("All deliveries deleted.");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting all entities:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
