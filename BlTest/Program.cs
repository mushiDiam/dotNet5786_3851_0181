using System;
using BlApi;
using BO;
using DO;

namespace BlTest
{
    internal class Program
    {
        // 1. Connection to BL via Factory
        static readonly BlApi.IBl s_bl = BlApi.Factory.Get();

        static void Main(string[] args)
        {
            Console.WriteLine("=== BL Test Application ===");

            while (true)
            {
                try
                {
                    // UX: Clear screen only when returning to main menu
                    Console.Clear();
                    Console.WriteLine("\n=== MAIN MENU ===");
                    Console.WriteLine("1. Orders");
                    Console.WriteLine("2. Couriers");
                    Console.WriteLine("3. Admin (Clock & Config)");
                    Console.WriteLine("0. Exit");

                    int choice = ReadInt("Choose option: ");

                    switch (choice)
                    {
                        case 1:
                            OrdersMenu();
                            break;
                        case 2:
                            CouriersMenu();
                            break;
                        case 3:
                            AdminMenu();
                            break;
                        case 0:
                            Console.WriteLine("Exiting...");
                            return;
                        default:
                            Console.WriteLine("Invalid choice.");
                            Pause();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Global Exception Handler
                    PrintExceptionInfo(ex);
                    Pause();
                }
            }
        }

        #region Orders Menu
        static void OrdersMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n--- Orders Menu ---");
                Console.WriteLine("1. Get orders list");
                Console.WriteLine("2. Get order details");
                Console.WriteLine("3. Add order");
                Console.WriteLine("4. Update order");
                Console.WriteLine("5. Cancel order");
                Console.WriteLine("0. Back");

                int choice = ReadInt("Choose option: ");
                if (choice == 0) return;

                try
                {
                    switch (choice)
                    {
                        case 1:
                            // We ask for Admin ID just in case BL requires permission validation
                            int adminId = ReadInt("Enter Admin ID (for permission): ");
                            var list = s_bl.Order.GetOrders(adminId, null, null, null);

                            Console.WriteLine("--- List of Orders ---");
                            foreach (var item in list)
                                Console.WriteLine(item);
                            break;

                        case 2:
                            int reqId = ReadInt("Requester ID: ");
                            int orderId = ReadInt("Enter Order ID: ");

                            var order = s_bl.Order.Details(reqId, orderId);
                            // Null check in case BL returns null instead of throwing
                            Console.WriteLine(order?.ToString() ?? "Order not found.");
                            break;

                        case 3:
                            int creatorId = ReadInt("Admin/Creator ID: ");
                            var newOrder = CollectOrderFromInput(isUpdate: false);
                            s_bl.Order.Add(creatorId, newOrder);
                            Console.WriteLine("Order added successfully.");
                            break;

                        case 4:
                            int updId = ReadInt("Admin ID: ");
                            var updOrder = CollectOrderFromInput(isUpdate: true);
                            s_bl.Order.UpdateDetails(updId, updOrder);
                            Console.WriteLine("Order updated successfully.");
                            break;

                        case 5:
                            int cancelReqId = ReadInt("Requester ID: ");
                            int cancelId = ReadInt("Enter Order ID to cancel: ");
                            s_bl.Order.Cancel(cancelReqId, cancelId);
                            Console.WriteLine("Cancel request sent.");
                            break;

                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                    Pause();
                }
                catch (Exception ex)
                {
                    PrintExceptionInfo(ex);
                    Pause();
                }
            }
        }

        static BO.Order CollectOrderFromInput(bool isUpdate)
        {
            BO.Order o;
            if (isUpdate)
            {
                int idToUpdate = ReadInt("Order ID to Update: ");
                o = new BO.Order { Id = idToUpdate };
            }
            else
            {
                o = new BO.Order { Id = 0 }; // 0 signals New to most systems
                Console.WriteLine("(ID will be generated automatically)");
            }

            Console.Write("Full address: ");
            o.FullAddress = Console.ReadLine() ?? "";

            o.Latitude = ReadDouble("Latitude: ");
            o.Longitude = ReadDouble("Longitude: ");

            Console.Write("Customer name: ");
            o.CustomerName = Console.ReadLine() ?? "";

            Console.Write("Customer phone: ");
            o.CustomerPhone = Console.ReadLine() ?? "";

            o.OrderType = ReadEnum<BO.OrderTypes>("Order Type");

            return o;
        }
        #endregion

        #region Couriers Menu
        static void CouriersMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n--- Couriers Menu ---");
                Console.WriteLine("1. Sign In (Enter Program)");
                Console.WriteLine("2. List couriers");
                Console.WriteLine("3. Courier details");
                Console.WriteLine("4. Add courier");
                Console.WriteLine("5. Update courier");
                Console.WriteLine("0. Back");

                int choice = ReadInt("Choose option: ");
                if (choice == 0) return;

                try
                {
                    switch (choice)
                    {
                        case 1:
                            int id = ReadInt("Courier ID: ");
                            // Assuming EnterProgram returns a Job/Role enum or object
                            var job = s_bl.Courier.EnterProgram(id);
                            Console.WriteLine($"Signed in successfully. Role: {job}");
                            break;

                        case 2:
                            int reqId = ReadInt("Admin ID: ");
                            var list = s_bl.Courier.GetCouriers(reqId, null, null);
                            Console.WriteLine("--- List of Couriers ---");
                            foreach (var c in list)
                                Console.WriteLine(c);
                            break;

                        case 3:
                            int requesterId = ReadInt("Requester ID: ");
                            int courierId = ReadInt("Courier ID: ");
                            var courier = s_bl.Courier.Details(requesterId, courierId);
                            Console.WriteLine(courier?.ToString() ?? "Courier not found.");
                            break;

                        case 4:
                            int adminId = ReadInt("Admin ID: ");
                            var newCourier = CollectCourierFromInput(isUpdate: false);
                            s_bl.Courier.Add(adminId, newCourier);
                            Console.WriteLine("Courier added.");
                            break;

                        case 5:
                            int admId = ReadInt("Admin ID: ");
                            var upd = CollectCourierFromInput(isUpdate: true);
                            s_bl.Courier.UpdateDetails(admId, upd);
                            Console.WriteLine("Courier updated.");
                            break;

                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                    Pause();
                }
                catch (Exception ex)
                {
                    PrintExceptionInfo(ex);
                    Pause();
                }
            }
        }

        static BO.Courier CollectCourierFromInput(bool isUpdate)
        {
            string prompt = isUpdate ? "Courier ID to Update: " : "New Courier ID (TZ): ";
            int id = ReadInt(prompt);

            var c = new BO.Courier
            {
                Id = id
            };

            Console.Write("Full name: ");
            c.FullName = Console.ReadLine() ?? "";

            Console.Write("Phone: ");
            c.PhoneNumber = Console.ReadLine() ?? "";

            Console.Write("Email: ");
            c.Email = Console.ReadLine() ?? "";

            c.IsActive = ReadBool("Is active? ");

            c.MaxDistancePreference = ReadDouble("Max Distance: ");

            c.Transport = ReadEnum<BO.Transportation>("Transportation Mode");

            return c;
        }
        #endregion

        #region Admin Menu
        static void AdminMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n--- Admin Menu ---");
                Console.WriteLine("1. Get System Clock");
                Console.WriteLine("2. Forward Clock");
                Console.WriteLine("3. Reset Database (Delete All)");
                Console.WriteLine("4. Initialize Database (Mock Data)");
                Console.WriteLine("0. Back");

                int choice = ReadInt("Choose option: ");
                if (choice == 0) return;

                try
                {
                    switch (choice)
                    {
                        case 1:
                            Console.WriteLine($"Current System Time: {s_bl.Admin.GetClock()}");
                            break;

                        case 2:
                            Console.WriteLine("How much time to advance?");
                            Console.WriteLine("For minute, press 1");
                            Console.WriteLine("For hour, press 2");
                            Console.WriteLine("For day, press 3");
                            Console.WriteLine("For month, press 4");
                            Console.WriteLine("For year, press 5");
                            switch(ReadInt("Your choice: "))
                            {
                                case 1:
                                    s_bl.Admin.ForwardClock(BO.Times.Minute);
                                    break;
                                case 2:
                                    s_bl.Admin.ForwardClock(BO.Times.Hour);
                                    break;
                                case 3:
                                    s_bl.Admin.ForwardClock(BO.Times.Day);
                                    break;
                                case 4:
                                    s_bl.Admin.ForwardClock(BO.Times.Month);
                                    break;
                                case 5:
                                    s_bl.Admin.ForwardClock(BO.Times.Year);
                                    break;
                                default:
                                   throw new BlInvalidValueException("Invalid time unit choice.");
                            }
                            Console.WriteLine($"Clock updated to: {s_bl.Admin.GetClock()}");
                            break;
     
                        case 3:
                            Console.WriteLine("WARNING: This will wipe all data.");
                            if (ReadBool("Are you sure?"))
                            {
                                s_bl.Admin.ResetDB();
                                Console.WriteLine("Database reset complete.");
                            }
                            break;

                        case 4:
                            s_bl.Admin.InitializeDB();
                            Console.WriteLine("Database initialized with Mock Data.");
                            break;

                        default:
                            Console.WriteLine("Invalid choice");
                            break;
                    }
                    Pause();
                }
                catch (Exception ex)
                {
                    PrintExceptionInfo(ex);
                    Pause();
                }
            }
        }
        #endregion

        #region Helper Methods
        static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out var v)) return v;
                Console.WriteLine("Invalid input. Please enter an integer.");
            }
        }

        static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (double.TryParse(Console.ReadLine(), out var v)) return v;
                Console.WriteLine("Invalid input. Please enter a number.");
            }
        }

        static bool ReadBool(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n): ");
                string s = Console.ReadLine()?.Trim().ToLower() ?? "";
                if (s == "y" || s == "yes" || s == "true" || s == "1") return true;
                if (s == "n" || s == "no" || s == "false" || s == "0") return false;
                Console.WriteLine("Invalid input. Type 'y' or 'n'.");
            }
        }

        // Generic Enum Reader with validation
        static T ReadEnum<T>(string prompt) where T : struct, Enum
        {
            Console.WriteLine($"--- Select {prompt} ---");
            // Display options
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                Console.WriteLine($"{(int)val}. {val}");
            }

            while (true)
            {
                int choice = ReadInt("Enter choice number: ");

                // Validate that the int is actually defined in the Enum
                if (Enum.IsDefined(typeof(T), choice))
                    return (T)Enum.ToObject(typeof(T), choice);

                Console.WriteLine("Invalid selection. Try again.");
            }
        }

        static void PrintExceptionInfo(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n=== EXCEPTION ===");
            Console.WriteLine($"Type:    {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner:   {ex.InnerException.Message}");
            }
            Console.WriteLine("=================");
            Console.ResetColor();
        }

        static void Pause()
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
        #endregion
    }
}