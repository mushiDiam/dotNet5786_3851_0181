using BO;
using DalApi;
using DO;
namespace Helpers;

internal static class OrderManager{
    private static IDal s_dal = Factory.Get;
    public static BO.Order ConvertToBO(DO.Order doOrder)
    {
        BO.Order boOrder = new BO.Order
        {
            Id = doOrder.Id,
            OrderType = (BO.OrderTypes)doOrder.OrderType,
            Latitude = doOrder.Latitude,
            Longitude = doOrder.Longtitude,
            AirDistance = GetAirDistance(doOrder.Latitude, doOrder.Longtitude, (double)s_dal.Config.CompanyLatitude, (double)s_dal.Config.CompanyLongitude),
            Weight = doOrder.Weight,
            Volume = doOrder.Volume,
            Fragile = doOrder.Fragile,
            CreatedAt = doOrder.CreatedAt,
            ExpectedDeliveryTime = doOrder.CreatedAt.Add(CalculateRemainingTime(doOrder.Id)),
            MaxDeliveryTime = doOrder.CreatedAt.Add(s_dal.Config.MaxDeliveryTime),
            EndType = CalculateEndType(doOrder.Id),
            ScheduleStatus = CalculateScheduleStatus(doOrder.Id, doOrder.CreatedAt),
            RemainingTime = CalculateRemainingTime(doOrder.Id),
            Deliveries = GetAllDeliveriesForOrder(doOrder.Id)
        };
        return boOrder;
    }

    private static EndTypes CalculateEndType(int orderId)
    {
        DO.Delivery del = DeliveryManager.GetDelivery(orderId);
        if (del == null)
            return EndTypes.InProgress;
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
            return EndTypes.Completed;
        else if (del.EndOfOrder == DO.EndOfOrder.Canceled)
            return EndTypes.Cancelled;
        else
            return EndTypes.InProgress;
    }

    /// <summary>
    /// Calculate schedule status (On time / Over time / In risk) using delivery timings and configured risk window.
    /// </summary>
    /// <param name="orderId">Order id.</param>
    /// <returns><see cref="ScheduleStatus"/> for the order.</returns>
    private static ScheduleStatuses CalculateScheduleStatus(int orderId, DateTime OrderTime)
    {
        DO.Delivery? del = DeliveryManager.GetDelivery(orderId);
        DateTime maxDeliveryTime = OrderTime.Add(s_dal.Config.MaxDeliveryTime);
        TimeSpan riskRange = s_dal.Config.RiskRange;

        // No delivery record => treat as open
        if (del == null)
        {
            if (DateTime.Now > maxDeliveryTime)
                return ScheduleStatuses.Late;

            var remainingToMax = maxDeliveryTime - DateTime.Now;
            return remainingToMax <= riskRange ? ScheduleStatuses.AtRisk : ScheduleStatuses.OnTime;
        }

        // Completed delivery -> compare end time against max allowed delivery time
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
        {
            if (del.TimeOfDelivery.HasValue && del.TimeOfDelivery.Value <= maxDeliveryTime)
                return ScheduleStatuses.OnTime;
            return ScheduleStatuses.Late;
        }

        // Delivery exists but not finished (in progress)
        if (DateTime.Now > maxDeliveryTime)
            return ScheduleStatuses.Late;

        var remaining = maxDeliveryTime - DateTime.Now;
        return remaining <= riskRange ? ScheduleStatuses.AtRisk : ScheduleStatuses.OnTime;
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
                    OrderStatus = d.EndOfOrder.HasValue ? (BO.OrderStatuses?)d.EndOfOrder.Value : null,
                    EndTime = d.TimeOfDelivery,
                    StartTime = d.StartOfDelivery,
                };
            })
            .ToList();

        return deliveries;
    }
}
