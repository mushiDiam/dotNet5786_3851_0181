/*namespace DalTest;
using DalApi;
using DO;

public static class Initialization
{
    private static IConfig? s_dalConfig;
    private static ICourier? s_dalCourier;
    private static IDelivery? s_dalDelivery;
    private static IOrder? s_dalOrder;

    private static readonly Random s_rand = new();
    private static void createCouriers()
    {
    }

    private static void createDeliveries()
    {
    }

    private static void createOrders()
    {
    }

}
*/


using System;
using System.Collections.Generic;
using System.Linq;
using DalApi;
using DO;

namespace Dal;

internal static class Initialization
{
    private static IConfig? s_dalConfig;
    private static IDelivery? s_dalDelivery;
    private static IOrder? s_dalOrder;
    private static ICourier? s_dalCourier;
    public static  void Do(IConfig _config, IDelivery _delivery, IOrder _order, ICourier _courier)
    {
        s_dalConfig = _config ?? throw new NullReferenceException("DAL Config cannot be null!");
        s_dalDelivery = _delivery ?? throw new NullReferenceException("DAL Delivery cannot be null!");
        s_dalOrder = _order ?? throw new NullReferenceException("DAL Order cannot be null!");
        s_dalCourier = _courier ?? throw new NullReferenceException("DAL Courier cannot be null!");
    

    }
    private static readonly Random s_rand = new();

    internal static void Do()
    {
        createCouriers();
        createOrders();
        createDeliveries();
    }

    private static void createCouriers()
    {
        var names = new[] {
            "David Levi", "Eli Cohen", "Shira Bar", "Maya Dan", "Lior Azulay",
            "Neta Ben", "Ron Cohen", "Adi Mor", "Dana Regev", "Itai Mor",
            "Noa Tal", "Avi Gold", "Tamar Alon", "Eden Shalev", "Liad Romano",
            "Gal Azulay", "Yael Amir", "Ben Shachar", "Omer Lavi", "Noga Arbel"
        };

        foreach (var name in names)
        {
            var id = s_rand.Next(200000000, 400000000);
            var orderType = (OrderType)s_rand.Next(0, 4);

            double maxDist = orderType switch
            {
                OrderType.Car => s_rand.NextDouble() * 15 + 15,
                OrderType.Motorcycle => s_rand.NextDouble() * 12 + 8,
                OrderType.Bike => s_rand.NextDouble() * 5 + 3,
                OrderType.Walking => s_rand.NextDouble() * 2 + 0.5,
                _ => 10
            };

            bool active = s_rand.NextDouble() < 0.75;
            DateTime startWork = DateTime.Now.AddDays(-s_rand.Next(100, 1000));

            var courier = new Courier(
                id,
                active,
                maxDist,
                startWork,
                orderType,
                name,
                "050-" + s_rand.Next(1000000, 9999999),
                $"{name.Replace(" ", ".").ToLower()}@mail.com",
                "1234"
            );

            s_dalCourier.Create(courier);
        }
    }

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

    private static void createOrders()
    {
        var customers = new[] {
            "Noa Tal", "Avi Gold", "Dana Regev", "Itai Mor", "Yasmin Levi",
            "Yossi Bar", "Roni Katz", "Adi Ben", "Eli Rahamim", "Nitzan Tzur"
        };

        int total = 50;

        for (int i = 0; i < total; i++)
        {
            var addr = addresses[s_rand.Next(addresses.Length)];
            var cust = customers[s_rand.Next(customers.Length)];
            var createdAt = DateTime.Now.AddDays(-s_rand.Next(1, 90));

            var order = new Order(
                Id: 0,
                AdderssOfOrder: addr.Address,
                Latitude: addr.Lat,
                Longtitude: addr.Lon,
                CustomerName: cust,
                CustomerPhone: "052-" + s_rand.Next(1000000, 9999999),
                CreatedAt: createdAt,
                Fragile: s_rand.Next(2) == 0,
                Weight: s_rand.Next(1, 20),
                Volume: s_rand.Next(1, 10)
            );

            s_dalOrder.Create(order);
        }
    }

    private static void createDeliveries()
    {
        foreach (var order in DataSource.Orders)
        {
            var availableCouriers = DataSource.Couriers
                .Where(c => Distance(order.Latitude, order.Longtitude, 32.0853, 34.7818) <= (c.MaxDeliveryDistance ?? 10))
                .ToList();

            if (!availableCouriers.Any()) continue;

            var courier = availableCouriers[s_rand.Next(availableCouriers.Count)];

            DateTime start = order.CreatedAt.AddHours(s_rand.Next(1, 48));
            bool finished = s_rand.NextDouble() < 0.6;
            DateTime? end = finished ? start.AddHours(s_rand.Next(1, 10)) : null;

            var delivery = new Delivery(
                Id: 0,
                OrderId: order.Id,
                CourierId: courier.Id,
                OrderType: courier.OrderType,
                StartOfDelivery: start,
                ActualDistance: Distance(order.Latitude, order.Longtitude, 32.0853, 34.7818),
                EndOfOrder: finished ? (EndOfOrder?)s_rand.Next(0, 5) : null,
                TimeOfDelivery: end
            );

            s_dalDelivery.Create(delivery);
        }
    }

    private static double Distance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371; // Earth radius (km)
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
