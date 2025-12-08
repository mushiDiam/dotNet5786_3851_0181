using System.Collections.Generic;
using System.Linq;
using DalApi;
using DO;
using BO;

namespace Helpers;

internal static class CourierManager
{
    private static IDal s_dal = Factory.Get;
    public static IEnumerable<BO.Courier> ReadAllCouriers(Func<BO.Courier, bool>? predicate = null)
    {
        IEnumerable<BO.Courier> boCouriers = s_dal.Courier.ReadAll().Select(ConvertDOTToBO);
        return predicate != null ? boCouriers.Where(predicate) : boCouriers;
    }
    public static BO.Courier ConvertDOTToBO(DO.Courier dalCourier)
    {
        BO.Courier boCourier = new BO.Courier
        {
            Id = dalCourier.Id,
            FullName = dalCourier.Name,
            PhoneNumber = dalCourier.Phone,
            IsActive = dalCourier.Active,
            Transport = (BO.Transportaion)dalCourier.OrderType,
            MaxDistancePreference = (double)dalCourier.MaxDeliveryDistance,
            Email = dalCourier.Email,
            Password = dalCourier.Password,
            JoinDate = dalCourier.JoinDate,
            DeliveryCountLate = CalculateLateOrders(dalCourier.Id),
            DeliveryCountOnTime = CalculateOnTimeOrders(dalCourier.Id),
            ActiveOrder = GetActiveOrder(dalCourier.Id)
        };
        return boCourier;
    }
    private static int CalculateLateOrders(int courierId)
    {
        List<BO.Order> orders = DeliveryManager.GetAllOrdersByCourier(courierId);
        int count = 0;
        foreach (BO.Order order in orders)
        {
            if (order.EndType == BO.EndTypes.Completed && order.MaxDeliveryTime < order.ExpectedDeliveryTime)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Count how many completed deliveries for the courier were in time.
    /// </summary>
    /// <param name="courierId">Courier id to analyze.</param>
    /// <returns>Number of in-time finished orders.</returns>
    private static int CalculateOnTimeOrders(int courierId)
    {
        List<BO.Order> orders = DeliveryManager.GetAllOrdersByCourier(courierId);
        int count = 0;
        foreach (BO.Order order in orders)
        {
            if (order.EndType == BO.EndTypes.Completed && order.MaxDeliveryTime >= order.ExpectedDeliveryTime)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Return the current order-in-progress for the courier, or null when none.
    /// The returned object contains delivery and order details useful for presentation.
    /// </summary>
    /// <param name="courierId">Courier id to query.</param>
    /// <returns><see cref="OrderInProgress"/> instance or null if no order is in progress.</returns>
    private static OrderInProgress GetActiveOrder(int courierId)
    {
        List<BO.Order> orders = DeliveryManager.GetAllOrdersByCourier(courierId);
        foreach (BO.Order order in orders)
        {
            if (order.EndType == BO.EndTypes.InProgress)
            {
                OrderInProgress oip = new OrderInProgress()
                {
                    DeliveryId = DeliveryManager.GetDelivery(order.Id).Id,
                    OrderId = order.Id,
                    OrderType = order.OrderType,
                    Description = order.Description,
                    Address = order.FullAddress,
                    AirDistance = order.AirDistance,
                    ActualDistance = DeliveryManager.GetDelivery(order.Id).ActualDistance,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    CreatedAt = order.CreatedAt,
                    StartDeliveryTime = DeliveryManager.GetDelivery(order.Id).StartOfDelivery,
                    ExpectedDeliveryTime = (System.DateTime) order.ExpectedDeliveryTime,
                    MaxiumDeliveryTime = order.MaxDeliveryTime,
                    EndType = order.EndType,
                    ScheduleStatus = order.ScheduleStatus,
                    TimeLeftForDelivery = order.RemainingTime
                };
                return oip;
            }
        }
        return null;
    }
}