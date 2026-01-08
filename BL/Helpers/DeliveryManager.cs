using System;
using DalApi;
using DO;
namespace Helpers;

internal static class DeliveryManager{
    private static IDal s_dal = Factory.Get;

    internal static ObserverManager Observers = new();
    public static List<BO.Order> GetAllOrdersByCourier(int courierId)
    {
        var deliveries = s_dal.Delivery.ReadAll(d => d.CourierId == courierId);
        List<BO.Order> orders = new List<BO.Order>();
        foreach (var delivery in deliveries)
        {
            var doOrder = s_dal.Order.Read(o => o.Id == delivery.OrderId);
            BO.Order boOrders = OrderManager.ConvertToBO(doOrder);
            orders.Add(boOrders);
        }
        return orders;
    }
    public static Delivery? GetDelivery(int orderId)
    {

        DO.Delivery? delToRet = null;
        try
        {
            delToRet = s_dal.Delivery.Read(d => d.OrderId == orderId);
        }
        catch(DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Delivery not found", ex);
        }
        return delToRet;
    }

    internal static void CancelDelivery(int deliveryId)
    {
        // Attempt to read the delivery entity
        var delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
        if (delivery is null)
            return; // nothing to cancel

        // Only cancel if the delivery is still in-progress (no EndOfOrder set)
        if (delivery.EndOfOrder.HasValue)
            return;

        // Use the DAL/system clock for consistency
        DateTime endTime = s_dal.Config.Clock;

        // Create updated delivery record (records support 'with' expressions)
        var updatedDelivery = delivery with
        {
            EndOfOrder = EndOfOrder.Canceled,
            TimeOfDelivery = endTime
        };

        // Request DAL to update the entity
        s_dal.Delivery.Update(updatedDelivery);
        Observers.NotifyItemUpdated(deliveryId);//stage 5
        Observers.NotifyListUpdated();//stage 5
    }
    public static List<DO.Delivery> GetAllDeliveryByCourier(int courierId)
    {
        var deliverys = s_dal.Delivery.ReadAll(d => d.CourierId == courierId);
        return deliverys.ToList();
    }
    public static void AssignDelivery(int orderId, int courierId)
    {
        DO.Delivery delivery = new DO.Delivery()
        {
            OrderId = orderId,
            CourierId = courierId,
            StartOfDelivery = DateTime.Now,
            TimeOfDelivery = null,
            EndOfOrder = null // Or whatever the nullable enum/status field is named
        };
        s_dal.Delivery.Create(delivery);
        Observers.NotifyListUpdated(); //stage 5
        Observers.NotifyItemUpdated(orderId);//stage 5

    }
    internal static void CreateMockDeliveryForCancellation(int orderId, DO.OrderType type)
    {
        DO.Delivery canceledDelivery = new DO.Delivery
        {
            OrderId = orderId,
            CourierId = 0,
            StartOfDelivery = DateTime.Now,
            TimeOfDelivery = DateTime.Now,
            EndOfOrder = DO.EndOfOrder.Canceled,
            ActualDistance = 0,
            OrderType = type

        };
        s_dal.Delivery.Create(canceledDelivery);
        Observers.NotifyListUpdated(); //stage 5
        Observers.NotifyItemUpdated(orderId);//stage 5
    }

    internal static void CancelActiveDelivery(int orderId)
    {
        // Logic specific to finding the active delivery is HIDDEN here
        // Note: Use d.Id if you have the delivery ID, or query by OrderId if that's the flow
        DO.Delivery delivery = s_dal.Delivery.Read(d => d.OrderId == orderId && d.EndOfOrder == null);

        DO.Delivery updated = delivery with
        {
            EndOfOrder = DO.EndOfOrder.Canceled,
            TimeOfDelivery = DateTime.Now
        };

        s_dal.Delivery.Update(updated);
        Observers.NotifyListUpdated(); //stage 5
        Observers.NotifyItemUpdated(delivery.Id);//stage 5
    }
}
