using System;
using DalApi;
using DO;
namespace Helpers;

internal static class DeliveryManager{
    private static IDal s_dal = Factory.Get;
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
    public static Delivery GetDelivery(int orderId)
    { 
        return s_dal.Delivery.Read(d => d.OrderId == orderId);
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
    }
}
