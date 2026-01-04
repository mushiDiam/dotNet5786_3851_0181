using BlApi;
using BlImplementation;
using BO;
using DalApi;
using DO;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
namespace Helpers;

internal static class OrderManager{
    private static IDal s_dal = DalApi.Factory.Get;

    internal static ObserverManager Observers = new();

    private static readonly HttpClient s_client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10) // Set global timeout here
    };
    static OrderManager()
    {
        // OSRM requires a User-Agent. Setting it here ensures it's always present.
        s_client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StudentProject/1.0");
    }
    public static BO.Order ConvertToBO(DO.Order doOrder)
    {
        BO.Order boOrder = new BO.Order
        {
            Id = doOrder.Id,
            OrderType = (BO.OrderTypes)doOrder.OrderType,
            Latitude = doOrder.Latitude,
            Longitude = doOrder.Longitude,
            AirDistance = GetAirDistance(doOrder.Latitude, doOrder.Longitude, (double)s_dal.Config.CompanyLatitude, (double)s_dal.Config.CompanyLongitude),
            Weight = doOrder.Weight,
            Volume = doOrder.Volume,
            Fragile = doOrder.Fragile,
            CreatedAt = doOrder.CreatedAt,
            ExpectedDeliveryTime = doOrder.CreatedAt.Add(CalculateRemainingTime(doOrder.Id)),
            MaxDeliveryTime = doOrder.CreatedAt.Add(s_dal.Config.MaxDeliveryTime),
            OrderStatus = CalculateOrderStatus(doOrder.Id),
            ScheduleStatus = CalculateScheduleStatus(doOrder.Id, doOrder.CreatedAt),
            RemainingTime = CalculateRemainingTime(doOrder.Id),
            Deliveries = GetAllDeliveriesForOrder(doOrder.Id)
        };
        return boOrder;
    }
    public static DO.Order ConvertToDal(BO.Order boOrder)
    {
        DO.Order doOrder = new DO.Order
        {
            Id = boOrder.Id,
            OrderType = (DO.OrderType)boOrder.OrderType,
            Latitude = boOrder.Latitude,
            Longitude = boOrder.Longitude,
            Weight = boOrder.Weight,
            Volume = boOrder.Volume,
            Fragile = boOrder.Fragile,
            CreatedAt = boOrder.CreatedAt,
            CustomerName = boOrder.CustomerName,
            CustomerPhone = boOrder.CustomerPhone,
            AdderssOfOrder = boOrder.FullAddress,
            Description = boOrder.Description,
        };
        return doOrder;
    }
    public static void CancelOrder(int orderId)
    {
        // 1. Get the Order (assuming Read throws if not found, based on critique)
        BO.Order boOrder = Read(orderId) ?? throw new BlDoesNotExistException($"Order {orderId} not found");

        // 2. Validate Status
        if (boOrder.OrderStatus == OrderStatus.Closed ||
            boOrder.OrderStatus == OrderStatus.Cancelled)
        {
            throw new BlInvalidOperationException("Cannot cancel an order that is already delivered or canceled");
        }

        // 3. Delegate Action to DeliveryManager
        try
        {
            if (boOrder.OrderStatus == OrderStatus.Open)
            {
                // Pass simple data types, not BO objects if possible
                DeliveryManager.CreateMockDeliveryForCancellation(orderId, (DO.OrderType)boOrder.OrderType);
            }
            else if (boOrder.OrderStatus == OrderStatus.InProgress)
            {
                DeliveryManager.CancelActiveDelivery(orderId);
            }
        }
        catch (DO.DalDoesNotExistException ex)
        {
            // Translate DAL errors to BL errors
            throw new BlDoesNotExistException($"Active delivery for order {orderId} not found", ex);
        }
        Observers.NotifyItemUpdated(orderId); //stage 5
        Observers.NotifyListUpdated(); //stage 5

    }
    private static OrderStatus CalculateOrderStatus(int orderId)
    {
        DO.Delivery del = DeliveryManager.GetDelivery(orderId);
        if (del == null)
            return OrderStatus.InProgress;
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
            return OrderStatus.Closed;
        else if (del.EndOfOrder == DO.EndOfOrder.Canceled)
            return OrderStatus.Cancelled;
        else
            return OrderStatus.InProgress;
    }

    /// <summary>
    /// Calculate schedule status (On time / Over time / In risk) using delivery timings and configured risk window.
    /// </summary>
    /// <param name="orderId">Order id.</param>
    /// <returns><see cref="ScheduleStatus"/> for the order.</returns>
    private static ScheduleStatus CalculateScheduleStatus(int orderId, DateTime OrderTime)
    {
        DO.Delivery? del = DeliveryManager.GetDelivery(orderId);
        DateTime maxDeliveryTime = OrderTime.Add(s_dal.Config.MaxDeliveryTime);
        TimeSpan riskRange = s_dal.Config.RiskRange;

        // No delivery record => treat as open
        if (del == null)
        {
            if (DateTime.Now > maxDeliveryTime)
                return ScheduleStatus.Late;

            var remainingToMax = maxDeliveryTime - DateTime.Now;
            return remainingToMax <= riskRange ? ScheduleStatus.InRisk : ScheduleStatus.OnTime;
        }

        // Completed delivery -> compare end time against max allowed delivery time
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
        {
            if (del.TimeOfDelivery.HasValue && del.TimeOfDelivery.Value <= maxDeliveryTime)
                return ScheduleStatus.OnTime;
            return ScheduleStatus.Late;
        }

        // Delivery exists but not finished (in progress)
        if (DateTime.Now > maxDeliveryTime)
            return ScheduleStatus.Late;

        var remaining = maxDeliveryTime - DateTime.Now;
        return remaining <= riskRange ? ScheduleStatus.InRisk : ScheduleStatus.OnTime;
    }
    private static double GetAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371; // Radius of the earth in km
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c; // Distance in km
        return distance;
    }
    private static double ToRadians(double deg)
    {
        return deg * (Math.PI / 180);
    }
    private static TimeSpan CalculateRemainingTime(int orderId)
    {

        DO.Delivery del = DeliveryManager.GetDelivery(orderId);
        if (del == null)
        {
            //order has no delivery yet
            return TimeSpan.Zero;
        }
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
        {
            return TimeSpan.Zero;
        }
        DO.OrderType shiftType = del.OrderType;
        double delSpeed = 0;
        switch (shiftType)
        {
            case DO.OrderType.Car:
                delSpeed = s_dal.Config.AverageCarSpeed;
                break;
            case DO.OrderType.Bike:
                delSpeed = s_dal.Config.AverageBikeSpeed;
                break;
            case DO.OrderType.Motorcycle:
                delSpeed = s_dal.Config.AverageMotorcycleSpeed;
                break;
            case DO.OrderType.Walking:
                delSpeed = s_dal.Config.AverageWalkingSpeed;
                break;

        }
        double distance = del.ActualDistance ?? 0;

        double hours = distance / delSpeed;

        return TimeSpan.FromHours(hours);
    }

    /// <summary>
    /// Return all deliveries (from the DAL) for the specified order id converted to
    /// <see cref="DeliveryPerOrderInList"/> instances for presentation/listing.
    /// </summary>
    /// <param name="orderId">Order id to query.</param>
    /// <returns>List of <see cref="DeliveryPerOrderInList"/> for the order.</returns>
    private static List<DeliveryPerOrderInList> GetAllDeliveriesForOrder(int orderId)
    {
        // use LINQ to read DAL deliveries for the order and map to BO.DeliveryPerOrderInList
        var deliveries = s_dal.Delivery.ReadAll(d => d.OrderId == orderId)
            .Select(d =>
            {
                var courier = s_dal.Courier.Read(c => c.Id == d.CourierId);
                return new DeliveryPerOrderInList
                {
                    DeliveryId = d.Id,
                    CourierId = d.CourierId,
                    CourierName = courier?.Name ?? string.Empty,
                    Transport = (BO.Transportation)d.OrderType,
                    OrderStatus = d.EndOfOrder.HasValue ? (BO.OrderStatus?)d.EndOfOrder.Value : null,
                    EndTime = d.TimeOfDelivery,
                    StartTime = d.StartOfDelivery,
                };
            })
            .ToList();

        return deliveries;
    }
    internal static void CreateOrder(BO.Order order)
    {
        if (order is null)
            throw new BlInvalidValueException("Order cannot be null.");

        if (string.IsNullOrWhiteSpace(order.FullAddress))
            throw new BlInvalidValueException("Order address is required.");

        if (string.IsNullOrWhiteSpace(order.CustomerName))
            throw new BlInvalidValueException("Recipient name is required.");

        if (double.IsNaN(order.Latitude) || order.Latitude < -90 || order.Latitude > 90 ||
            double.IsNaN(order.Longitude) || order.Longitude < -180 || order.Longitude > 180)
            throw new BlInvalidValueException("Order coordinates are invalid.");
        DO.Order doOrder = ConvertToDal(order);
        try
        {
            s_dal.Order.Create(doOrder);
        }
        catch (DalAlreadyExistsException ex)
        {
            throw new BlAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
        }
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static BO.Order? Read(int orderId)
    {
        DO.Order doOrder;
        try
        {
            doOrder = s_dal.Order.Read(orderId);
        }
        catch(DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {orderId} does not exist.", ex);
        }
        if (doOrder is null)
            return null;
        return ConvertToBO(doOrder);
    }
    internal static void Update(BO.Order order)
    {
        if (order is null)
            throw new BlInvalidValueException("Order cannot be null.");
        DO.Order doOrder = ConvertToDal(order);
        try
        {
            s_dal.Order.Update(doOrder);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
        }
        Observers.NotifyItemUpdated(order.Id); //stage 5
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static void Delete(int orderId)
    {
        try
        {
            s_dal.Order.Delete(orderId);
        }
        catch (DalDoesNotExistException exception)
        {
            throw new BlInvalidOperationException($"Order ID {orderId} does not exist.", exception);
        }
        Observers.NotifyItemUpdated(orderId); //stage 5
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null)
    {
        var doOrders = s_dal.Order.ReadAll();
        var boOrders = doOrders.Select(doOrder => ConvertToBO(doOrder));
        if (filter != null)
        {
            boOrders = boOrders.Where(filter);
        }
        return boOrders;
    }
    internal static void DeleteAll()
    {
        s_dal.Order.DeleteAll();
        Observers.NotifyListUpdated(); //stage 5
    }
    public static async Task<double?> GetActualDistanceAsync(double latitude, double longitude, BO.Transportation transport)
    {
        // --- Validation (Your code is good here) ---
        if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
            latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new BlInvalidValueException("Order coordinates are invalid.");

        if (double.IsNaN((double)s_dal.Config.CompanyLatitude) || double.IsNaN((double)s_dal.Config.CompanyLongitude))
            throw new BlInvalidValueException("Company coordinates are not configured.");

        // --- Profile Mapping ---
        string profile = transport switch
        {
            BO.Transportation.Car => "car",
            BO.Transportation.Motorcycle => "car",
            BO.Transportation.Bike => "bike",
            BO.Transportation.Walking => "walking",
            _ => throw new BlInvalidValueException("Invalid transportation type")
        };

        // --- URL Construction ---
        string coordinates =
            $"{longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)};" +
            $"{Convert.ToDouble(s_dal.Config.CompanyLongitude).ToString(CultureInfo.InvariantCulture)},{Convert.ToDouble(s_dal.Config.CompanyLatitude).ToString(CultureInfo.InvariantCulture)}";

        string url = $"https://router.project-osrm.org/table/v1/{profile}/{coordinates}?annotations=distance";

        try
        {
            // 3. REUSE the static client (Do NOT use 'using' here)
            string json = await s_client.GetStringAsync(url);

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("code", out var codeProp) && codeProp.GetString() != "Ok")
                return null;

            var distances = root.GetProperty("distances");

            if (distances.GetArrayLength() == 0 || distances[0].GetArrayLength() < 2)
                return null;

            double meters = distances[0][1].GetDouble();
            return Math.Round(meters / 1000.0, 2);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            // This catches the Timeout
            return null;
        }
    }
    public static async Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return (null, null);

        string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

        try
        {
            if (!s_client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                s_client.DefaultRequestHeaders.Add("User-Agent", "CouriersApp/1.0 (myemail@example.com)");
            }

            string json = await s_client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetArrayLength() > 0)
            {
                var location = root[0];
                double lat = double.Parse(location.GetProperty("lat").GetString()!);
                double lon = double.Parse(location.GetProperty("lon").GetString()!);
                return (lat, lon);
            }
        }
        catch
        {
        }

        return (null, null);
    }
    private static OrderInList ConvertToOrderInList(BO.Order boOrder)
    {
        TimeSpan completionTime = TimeSpan.Zero;
        DO.Delivery? del = DeliveryManager.GetDelivery(boOrder.Id);
        if (del != null && del.EndOfOrder == DO.EndOfOrder.Completed && del.TimeOfDelivery.HasValue)
        {
            completionTime = del.TimeOfDelivery.Value - boOrder.CreatedAt;
        }
        return new OrderInList
        {
            DeliveryId = del?.Id,
            OrderId = boOrder.Id,
            OrderType = boOrder.OrderType,
            AirDistance = boOrder.AirDistance,
            OrderStatus= boOrder.OrderStatus,
            ScheduleStatus = boOrder.ScheduleStatus,
            RemainingTime = boOrder.RemainingTime,
            CompletionTime = completionTime,
            DeliveryCount = boOrder.Deliveries?.Count ?? 0
        };
    }
    public static IEnumerable<OrderInList> GetOrders(OrderInListOptions? filter, object? obj, OrderInListOptions? sort)
    {
        // 1. DEFERRED EXECUTION
        IEnumerable<BO.Order> boOrders = ReadAll();

        // 2. PRE-LOAD DELIVERIES (The N+1 Fix)
        var allDeliveries = s_dal.Delivery.ReadAll()
            .GroupBy(d => d.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 3. FILTERING
        if (filter.HasValue && obj != null)
        {
            switch (filter)
            {
                case OrderInListOptions.DeliveryId:
                    if (obj is int deliveryId)
                    {
                        boOrders = boOrders.Where(o =>
                            allDeliveries.TryGetValue(o.Id, out var dels) &&
                            dels.Any(d => d.Id == deliveryId)
                        );
                    }
                    else throw new BlInvalidOperationException("Filter value for DeliveryId must be an integer");
                    break;

                case OrderInListOptions.OrderId:
                    if (obj is int orderId)
                        boOrders = boOrders.Where(o => o.Id == orderId);
                    break;

                case OrderInListOptions.OrderType:
                    if (obj is BO.OrderTypes type)
                        boOrders = boOrders.Where(o => o.OrderType == type);
                    break;

                case OrderInListOptions.AirDistance:
                    if (obj is double maxDistance)
                        boOrders = boOrders.Where(o => o.AirDistance <= maxDistance);
                    break;

                case OrderInListOptions.OrderStatus:
                    if (obj is BO.OrderStatus status)
                        boOrders = boOrders.Where(o => o.OrderStatus == status);
                    break;

                case OrderInListOptions.ScheduleStatus:
                    if (obj is BO.ScheduleStatus schedStatus)
                        boOrders = boOrders.Where(o => o.ScheduleStatus == schedStatus);
                    break;

                case OrderInListOptions.RemainingTime:
                    if (obj is TimeSpan maxRemaining)
                        boOrders = boOrders.Where(o => o.RemainingTime <= maxRemaining);
                    break;

                case OrderInListOptions.CompletionTime:
                    if (obj is TimeSpan maxTime)
                    {
                        boOrders = boOrders.Where(o =>
                        {
                            var time = GetCompletionTime(o.Id, o.CreatedAt, allDeliveries);
                            return time != null && time <= maxTime;
                        });
                    }
                    break;

                case OrderInListOptions.DeliveryCount:
                    if (obj is int minCount)
                        boOrders = boOrders.Where(o =>
                            allDeliveries.TryGetValue(o.Id, out var dels) && dels.Count >= minCount
                        );
                    break;

                default:
                    break;
            }
        }

        // 4. SORTING
        if (!sort.HasValue)
        {
            boOrders = boOrders.OrderBy(o => o.OrderStatus);
        }
        else
        {
            switch (sort)
            {
                case OrderInListOptions.DeliveryId:
                    boOrders = boOrders.OrderBy(o =>
                        // Use TryGetValue logic inline or helper if complex
                        allDeliveries.TryGetValue(o.Id, out var dels)
                            ? dels.OrderByDescending(d => d.StartOfDelivery).FirstOrDefault()?.Id ?? int.MaxValue
                            : int.MaxValue
                    );
                    break;

                case OrderInListOptions.OrderId:
                    boOrders = boOrders.OrderBy(o => o.Id);
                    break;

                case OrderInListOptions.OrderType:
                    boOrders = boOrders.OrderBy(o => o.OrderType);
                    break;

                case OrderInListOptions.AirDistance:
                    boOrders = boOrders.OrderBy(o => o.AirDistance);
                    break;

                case OrderInListOptions.OrderStatus:
                    boOrders = boOrders.OrderBy(o => o.OrderStatus);
                    break;

                case OrderInListOptions.ScheduleStatus:
                    boOrders = boOrders.OrderBy(o => o.ScheduleStatus);
                    break;

                case OrderInListOptions.RemainingTime:
                    boOrders = boOrders.OrderBy(o => o.RemainingTime);
                    break;

                case OrderInListOptions.CompletionTime:
                    boOrders = boOrders.OrderBy(o =>
                        GetCompletionTime(o.Id, o.CreatedAt, allDeliveries) ?? TimeSpan.MaxValue
                    );
                    break;

                case OrderInListOptions.DeliveryCount:
                    boOrders = boOrders.OrderBy(o =>
                        allDeliveries.TryGetValue(o.Id, out var dels) ? dels.Count : 0
                    );
                    break;

                default:
                    boOrders = boOrders.OrderBy(o => o.OrderStatus);
                    break;
            }
        }

        // 5. PROJECTION
        foreach (var order in boOrders)
        {
            // Use TryGetValue efficiently
            var deliveriesForOrder = allDeliveries.TryGetValue(order.Id, out var dels)
                ? dels
                : new List<DO.Delivery>();

            yield return ConvertToOrderInListCached(order, deliveriesForOrder, allDeliveries);
        }
    }
    private static TimeSpan? GetCompletionTime(int orderId, DateTime createdAt, Dictionary<int, List<DO.Delivery>> cache)
    {
        // Fixes Critique #2: Use TryGetValue
        if (!cache.TryGetValue(orderId, out var dels)) return null;

        var lastCompleted = dels
            .Where(d => d.EndOfOrder == DO.EndOfOrder.Completed && d.TimeOfDelivery.HasValue)
            .OrderByDescending(d => d.TimeOfDelivery)
            .FirstOrDefault();

        return lastCompleted?.TimeOfDelivery!.Value - createdAt;
    }
    private static OrderInList ConvertToOrderInListCached(BO.Order order, List<DO.Delivery> deliveries, Dictionary<int, List<DO.Delivery>> cache) // Pass cache if needed for helper reuse
    {
        var latestDelivery = deliveries
            .OrderByDescending(d => d.StartOfDelivery)
            .FirstOrDefault();

        // REUSE the logic helper here instead of rewriting it
        // Fixes Critique #3: Logic duplicated -> Logic reused
        TimeSpan completionTime = GetCompletionTime(order.Id, order.CreatedAt, cache) ?? TimeSpan.Zero;

        return new OrderInList
        {
            OrderId = order.Id,
            DeliveryId = latestDelivery?.Id,
            OrderType = order.OrderType,
            AirDistance = order.AirDistance,
            OrderStatus = order.OrderStatus,
            ScheduleStatus = order.ScheduleStatus,
            RemainingTime = order.RemainingTime,
            CompletionTime = completionTime,
            DeliveryCount = deliveries.Count
        };
    }
    internal static void EndDelivery(int courierId, int deliveryId)
    {

        DO.Delivery delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
        if (delivery == null)
            throw new BlDoesNotExistException($"Delivery {deliveryId} not found");
        DO.Delivery updatedDelivery = delivery with
        {
            EndOfOrder = DO.EndOfOrder.Completed, // Set status
            TimeOfDelivery = DateTime.Now         // Set time
        };

        // 4. Update DAL
        s_dal.Delivery.Update(updatedDelivery);
        Observers.NotifyItemUpdated(deliveryId);//stage 5
        Observers.NotifyListUpdated();//stage 5
    }
}
