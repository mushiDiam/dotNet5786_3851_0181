using System;
using System.Collections.Generic;
using System.Linq;
using DalApi;
using DO;

namespace DalTest;

/// <summary>
/// Provides database initialization functionality for the DAL test project.
/// Creates sample data for couriers, orders, and deliveries.
/// </summary>
public static class Initialization
{
    // DAL interface reference - initialized in Do() method
    // Previous implementation used separate interfaces for each entity
    /*private static IConfig? s_dalConfig;
    private static IDelivery? s_dalDelivery;
    private static IOrder? s_dalOrder;
    private static ICourier? s_dalCourier;*/
    private static IDal? s_dal;

    /// <summary>
    /// Main initialization method. Sets up the DAL, resets the database,
    /// configures manager credentials, and populates sample data.
    /// </summary>
    /// <remarks>
    /// Stage 4 implementation uses Factory pattern instead of direct DAL reference.
    /// </remarks>
    //public static  void Do(IDal dal)
    public static void Do() //stage 4

    {
        // Previous implementation required passing individual DAL references
        /*s_dalConfig = _config ?? throw new NullReferenceException("DAL Config cannot be null!");
        s_dalDelivery = _delivery ?? throw new NullReferenceException("DAL Delivery cannot be null!");
        s_dalOrder = _order ?? throw new NullReferenceException("DAL Order cannot be null!");
        s_dalCourier = _courier ?? throw new NullReferenceException("DAL Courier cannot be null!");*/
        //s_dal = dal ?? throw new DalCannotBeNullException("DAL object cannot be null!");
        
        // Stage 4: Get DAL instance from factory
        s_dal = DalApi.Factory.Get;
        
        // Reset database to clean state before initialization
        s_dal.resetDB();
        
        // Configure manager credentials for authentication
        s_dal.Config.ManagerId = 214633851;
        s_dal.Config.ManagerPassword = "10";
        
        // Populate sample data
        DataInit();

    }
    
    // Random number generator for creating varied sample data
    private static readonly Random s_rand = new();

    /// <summary>
    /// Populates the database with sample couriers, orders, and deliveries.
    /// </summary>
    internal static void DataInit()
    {
        createCouriers();
        createOrders();
        createDeliveries();
    }

    /// <summary>
    /// Creates 20 sample couriers with randomized attributes.
    /// Each courier gets a random transport type, max distance, and active status.
    /// </summary>
    private static void createCouriers()
    {
        // Sample Israeli names for couriers
        var names = new[] {
            "David Levi", "Eli Cohen", "Shira Bar", "Maya Dan", "Lior Azulay",
            "Neta Ben", "Ron Cohen", "Adi Mor", "Dana Regev", "Itai Mor",
            "Noa Tal", "Avi Gold", "Tamar Alon", "Eden Shalev", "Liad Romano",
            "Gal Azulay", "Yael Amir", "Ben Shachar", "Omer Lavi", "Noga Arbel"
        };

        foreach (var name in names)
        {
            // Generate random 9-digit ID in valid Israeli ID range
            var id = s_rand.Next(200000000, 400000000);
            
            // Randomly assign transport type
            var orderType = (OrderType)s_rand.Next(0, 4);

            // Max delivery distance varies by transport type
            // Cars can travel further than bicycles or walking
            double maxDist = orderType switch
            {
                OrderType.Car => s_rand.NextDouble() * 15 + 15,        // 15-30 km
                OrderType.Motorcycle => s_rand.NextDouble() * 12 + 8,  // 8-20 km
                OrderType.Bike => s_rand.NextDouble() * 5 + 3,         // 3-8 km
                OrderType.Walking => s_rand.NextDouble() * 2 + 0.5,    // 0.5-2.5 km
                _ => 10
            };

            // 75% of couriers are active
            bool active = s_rand.NextDouble() < 0.75;
            
            // Random join date between 100-1000 days ago
            DateTime startWork = DateTime.Now.AddDays(-s_rand.Next(100, 1000));

            var courier = new Courier(
                id,
                active,
                maxDist,
                startWork,
                orderType,
                name,
                "050-" + s_rand.Next(1000000, 9999999),  // Israeli mobile format
                $"{name.Replace(" ", ".").ToLower()}@mail.com",
                "1234"  // Default password
            );

            s_dal!.Courier.Create(courier);
        }
    }

    /// <summary>
    /// Predefined addresses in Tel Aviv with their coordinates.
    /// Used for generating sample orders with realistic Israeli locations.
    /// </summary>
    private static readonly (string Address, double Lat, double Lon)[] addresses = new[] {
        ("Rothschild Blvd 1, Tel Aviv", 32.062, 34.770),
        ("Ibn Gabirol 50, Tel Aviv", 32.081, 34.781),
        ("Dizengoff 150, Tel Aviv", 32.089, 34.776),
        ("Namir 122, Tel Aviv", 32.095, 34.789),
        ("Yigal Alon 90, Tel Aviv", 32.072, 34.794),
        ("Arlozorov 100, Tel Aviv", 32.084, 34.782),
        ("HaYarkon 200, Tel Aviv", 32.089, 34.769),
        ("Allenby 40, Tel Aviv", 32.063, 34.771),
        ("King George 30, Tel Aviv", 32.072, 34.776),
        ("HaMasger 9, Tel Aviv", 32.062, 34.781)
    };

    /// <summary>
    /// Creates 50 sample orders with randomized attributes.
    /// Orders are assigned random addresses, customers, and properties.
    /// </summary>
    private static void createOrders()
    {
        // Sample customer names
        var customers = new[] {
            "Noa Tal", "Avi Gold", "Dana Regev", "Itai Mor", "Yasmin Levi",
            "Yossi Bar", "Roni Katz", "Adi Ben", "Eli Rahamim", "Nitzan Tzur"
        };

        int total = 50;

        for (int i = 0; i < total; i++)
        {
            // Pick random address and customer
            var addr = addresses[s_rand.Next(addresses.Length)];
            var cust = customers[s_rand.Next(customers.Length)];
            
            // Orders created within the last 80 minutes
            var createdAt = DateTime.Now.AddMinutes(-s_rand.Next(0,80));

            var order = new Order(
                Id: 0,  // DAL will assign the next available ID
                Address: addr.Address,
                Latitude: addr.Lat,
                Longitude: addr.Lon,
                CustomerName: cust,
                CustomerPhone: "052-" + s_rand.Next(1000000, 9999999),
                CreatedAt: createdAt,
                Fragile: s_rand.Next(2) == 0,  // 50% chance of being fragile
                OrderType: (OrderType)s_rand.Next(0, 4),
                Weight: s_rand.Next(1, 20),    // 1-20 kg
                Volume: s_rand.Next(1, 10)     // 1-10 liters
            );

            s_dal!.Order.Create(order);
        }
    }

    /// <summary>
    /// Creates deliveries for the first 20 orders.
    /// Assigns random active couriers to orders and simulates delivery progress.
    /// </summary>
    private static void createDeliveries()
    {
        // Process only the first 20 orders
        foreach (var order in s_dal!.Order.ReadAll().Take(20))
        {
            // Get all active couriers for assignment
            var availableCouriers = s_dal!.Courier.ReadAll().Where(c=>c.Active);

            // Skip if no couriers available
            if (!availableCouriers.Any()) continue;

            // Randomly select a courier
            int count = s_rand.Next(availableCouriers.Count());
            var courier = availableCouriers.ElementAt(count);

            // Delivery starts 1-48 hours after order creation
            DateTime start = order.CreatedAt.AddHours(s_rand.Next(1, 48));
            
            // 60% of deliveries are completed
            bool finished = s_rand.NextDouble() < 0.6;
            DateTime? end = finished ? start.AddHours(s_rand.Next(1, 10)) : null;

            var delivery = new Delivery(
                Id: 0,  // DAL will assign the next available ID
                OrderId: order.Id,
                CourierId: courier.Id,
                OrderType: courier.OrderType,
                StartOfDelivery: start,
                // Calculate actual distance from order location to company headquarters (Tel Aviv center)
                ActualDistance: Distance(order.Latitude, order.Longitude, 32.0853, 34.7818),
                EndOfOrder: finished ? (EndOfOrder?)s_rand.Next(0, 5) : null,
                TimeOfDelivery: end
            );

            s_dal!.Delivery.Create(delivery);
        }
    }

    /// <summary>
    /// Calculates the Haversine distance between two geographic coordinates.
    /// This gives the "as the crow flies" distance in kilometers.
    /// </summary>
    /// <param name="lat1">Latitude of first point</param>
    /// <param name="lon1">Longitude of first point</param>
    /// <param name="lat2">Latitude of second point</param>
    /// <param name="lon2">Longitude of second point</param>
    /// <returns>Distance in kilometers</returns>
    private static double Distance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371; // Earth's radius in kilometers
        
        // Convert latitude and longitude differences to radians
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        
        // Haversine formula
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c; // Distance in km
    }
}