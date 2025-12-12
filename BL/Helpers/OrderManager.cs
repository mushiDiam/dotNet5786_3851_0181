using System.Globalization;
using System.Text.Json;
using BlApi;
using BLImplementation;
using BO;
using DalApi;
using DO;
namespace Helpers;

internal static class OrderManager{
    private static IDal s_dal = DalApi.Factory.Get;
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
        BO.Order? boOrder = Read(orderId);
        if(boOrder is null)
            throw new BlDoesNotExistException($"Order ID {orderId} does not exist.");
        if(boOrder.OrderStatus == OrderStatus.InProgress)
        {
            DO.Delivery? del = DeliveryManager.GetDelivery(orderId);
            if (del != null && del.EndOfOrder != DO.EndOfOrder.Completed)
            {
                DeliveryManager.CancelDelivery(del.Id);
            }
            else
            {
                DO.Delivery newDelivery = new DO.Delivery
                {
                    OrderId = orderId,
                    CourierId = 0,
                    OrderType = (DO.OrderType)boOrder.OrderType,
                    StartOfDelivery = DateTime.Now,
                    EndOfOrder = DO.EndOfOrder.Canceled,
                    TimeOfDelivery = DateTime.Now,
                    ActualDistance = 0,
                };
                s_dal.Delivery.Create(newDelivery);
            }
        }
        else
        {
            throw new BLInvalidValueException($"Order ID {orderId} is already completed or cancelled.");
        }
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
                    DeliveryType = (BO.DeliveryTypes)d.OrderType,
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
            throw new BLInvalidValueException("Order cannot be null.");

        if (string.IsNullOrWhiteSpace(order.FullAddress))
            throw new BLInvalidValueException("Order address is required.");

        if (string.IsNullOrWhiteSpace(order.CustomerName))
            throw new BLInvalidValueException("Recipient name is required.");

        if (double.IsNaN(order.Latitude) || order.Latitude < -90 || order.Latitude > 90 ||
            double.IsNaN(order.Longitude) || order.Longitude < -180 || order.Longitude > 180)
            throw new BLInvalidValueException("Order coordinates are invalid.");
        DO.Order doOrder = ConvertToDal(order);
        try
        {
            s_dal.Order.Create(doOrder);
        }
        catch (BlAlreadyExistsException ex)
        {
            throw new BlAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
        }
    }
    internal static BO.Order? Read(int orderId)
    {
        DO.Order doOrder;
        doOrder = s_dal.Order.Read(orderId);
        if (doOrder is null)
            return null;
        return ConvertToBO(doOrder);
    }
    internal static void Update(BO.Order order)
    {
        if (order is null)
            throw new BLInvalidValueException("Order cannot be null.");
        DO.Order doOrder = ConvertToDal(order);
        try
        {
            s_dal.Order.Update(doOrder);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
        }
    }
    internal static void Delete(int orderId)
    {
        try
        {
            s_dal.Order.Delete(orderId);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {orderId} does not exist.", ex);
        }
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
    }

    public static double? GetActualDistance(double latitude, double longitude, BO.Transportation transport)
    {
        if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
            latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new BLInvalidValueException("Order coordinates are invalid.");

        if (double.IsNaN((double)s_dal.Config.CompanyLatitude) || double.IsNaN((double)s_dal.Config.CompanyLongitude))
            throw new BLInvalidValueException("Company coordinates are not configured.");

        // Map ShiftType → OSRM profile
        string profile = transport switch
        {
            BO.Transportation.Car => "car",
            BO.Transportation.Motorcycle => "car",
            BO.Transportation.Bike => "bike",
            BO.Transportation.Walking => "walking",
            _ => "car"
        };

        // OSRM format : lon,lat;lon,lat
        string coordinates =
            $"{longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)};" +
            $"{Convert.ToString(s_dal.Config.CompanyLongitude, CultureInfo.InvariantCulture)},{Convert.ToString(s_dal.Config.CompanyLatitude, CultureInfo.InvariantCulture)}";

        string url = $"https://router.project-osrm.org/table/v1/{profile}/{coordinates}?annotations=distance";

        try
        {
            using HttpClient client = new();
            string json = client.GetStringAsync(url).GetAwaiter().GetResult();

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("code", out var codeProp) &&
                codeProp.GetString() != "Ok")
                return null;

            var distances = root.GetProperty("distances");

            if (distances.GetArrayLength() == 0 ||
                distances[0].GetArrayLength() < 2)
                return null;

            // Distance A → B in meters
            double meters = distances[0][1].GetDouble();
            return Math.Round(meters / 1000.0, 2);
        }
        catch
        {
            return null;
        }
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
    internal static IEnumerable<OrderInList> GetOrders(OrderInListOptions? filter, object? obj, OrderInListOptions? sort)
    {
        IEnumerable<BO.Order> boOrder = ReadAll();
        switch (filter)
        {
            case OrderInListOptions.DeliveryId:
                int deliveryId = (int)obj!;
                boOrder = boOrder.Where(o => o.Deliveries != null && o.Deliveries.Any(d => d.DeliveryId == deliveryId));
                break;
            case OrderInListOptions.OrderId:
                int orderId = (int)obj!;
                boOrder = boOrder.Where(o => o.Id == orderId);
                break;
            case OrderInListOptions.OrderType:
                OrderTypes orderType = (OrderTypes)obj!;
                boOrder = boOrder.Where(o => o.OrderType == orderType);
                break;
            case OrderInListOptions.AirDistance:
                double airDistance = (double)obj!;
                boOrder = boOrder.Where(o => o.AirDistance <= airDistance);
                break;
            case OrderInListOptions.OrderStatus:
                OrderStatus orderStatus = (OrderStatus)obj!;
                boOrder = boOrder.Where(o => o.OrderStatus == orderStatus);
                break;
            case OrderInListOptions.ScheduleStatus:
                ScheduleStatus scheduleStatus = (ScheduleStatus)obj!;
                boOrder = boOrder.Where(o => o.ScheduleStatus == scheduleStatus);
                break;
            case OrderInListOptions.RemainingTime:
                TimeSpan remainingTime = (TimeSpan)obj!;
                boOrder = boOrder.Where(o => o.RemainingTime <= remainingTime);
                break;
            case OrderInListOptions.CompletionTime:
                TimeSpan completionTime = (TimeSpan)obj!;
                boOrder = boOrder.Where(o =>
                {
                    DO.Delivery? del = DeliveryManager.GetDelivery(o.Id);
                    if (del != null && del.EndOfOrder == DO.EndOfOrder.Completed && del.TimeOfDelivery.HasValue)
                    {
                        TimeSpan orderCompletionTime = del.TimeOfDelivery.Value - o.CreatedAt;
                        return orderCompletionTime <= completionTime;
                    }
                    return false;
                });
                break;
            case OrderInListOptions.DeliveryCount:
                int deliveryCount = (int)obj!;
                boOrder = boOrder.Where(o => (o.Deliveries?.Count ?? 0) >= deliveryCount);
                break;
            default:
                break;
        }

        switch (sort) { 
            case OrderInListOptions.DeliveryId:
                boOrder = boOrder.OrderBy(o => o.Deliveries != null ? o.Deliveries.Min(d => d.DeliveryId) : int.MaxValue);
                break;
            case OrderInListOptions.OrderId:
                boOrder = boOrder.OrderBy(o => o.Id);
                break;
            case OrderInListOptions.OrderType:
                boOrder = boOrder.OrderBy(o => o.OrderType);
                break;
            case OrderInListOptions.AirDistance:
                boOrder = boOrder.OrderBy(o => o.AirDistance);
                break;
            case OrderInListOptions.OrderStatus:
                boOrder = boOrder.OrderBy(o => o.OrderStatus);
                break;
            case OrderInListOptions.ScheduleStatus:
                boOrder = boOrder.OrderBy(o => o.ScheduleStatus);
                break;
            case OrderInListOptions.RemainingTime:
                boOrder = boOrder.OrderBy(o => o.RemainingTime);
                break;
            case OrderInListOptions.CompletionTime:
                boOrder = boOrder.OrderBy(o =>
                {
                    DO.Delivery? del = DeliveryManager.GetDelivery(o.Id);
                    if (del != null && del.EndOfOrder == DO.EndOfOrder.Completed && del.TimeOfDelivery.HasValue)
                    {
                        return del.TimeOfDelivery.Value - o.CreatedAt;
                    }
                    return TimeSpan.MaxValue;
                });
                break;
            case OrderInListOptions.DeliveryCount:
                boOrder = boOrder.OrderBy(o => o.Deliveries?.Count ?? 0);
                break;
            default:
                break;
        }
        foreach (var order in boOrder)
            yield return ConvertToOrderInList(order);

    }

    internal static void EndDelivery(int courierId, int deliveryId)
    {

        DO.Delivery delivery = s_dal.Delivery.Read(d => d.OrderId == deliveryId);
        if (delivery == null)
            throw new BlDoesNotExistException($"Delivery {deliveryId} not found");
        DO.Delivery updatedDelivery = delivery with
        {
            EndOfOrder = DO.EndOfOrder.Completed, // Set status
            TimeOfDelivery = DateTime.Now         // Set time
        };

        // 4. Update DAL
        s_dal.Delivery.Update(updatedDelivery);
    }
}
