using DalApi;
using DO;
using System;
using System.Linq;
using DalApi;
using DO;
using BO;
namespace Helpers;

internal static class DeliveryManager
{
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
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Delivery not found", ex);
        }
        return delToRet;
    }

    internal static void CancelDelivery(int deliveryId)
    {
        var delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
        if (delivery is null) return;

        if (delivery.EndOfOrder.HasValue) return;

        var updatedDelivery = delivery with
        {
            EndOfOrder = EndOfOrder.Canceled,
            TimeOfDelivery = s_dal.Config.Clock
        };

        s_dal.Delivery.Update(updatedDelivery);

        Observers.NotifyListUpdated();
        Observers.NotifyItemUpdated(deliveryId);

        try { OrderManager.Observers.NotifyItemUpdated(updatedDelivery.OrderId); } catch { }
        try { if (updatedDelivery.CourierId != 0) CourierManager.Observers.NotifyItemUpdated(updatedDelivery.CourierId); } catch { }
    }
    public static List<DO.Delivery> GetAllDeliveryByCourier(int courierId)
    {
        var deliverys = s_dal.Delivery.ReadAll(d => d.CourierId == courierId);
        return deliverys.ToList();
    }
    public static void AssignDelivery(int orderId, int courierId)
    {
        // Validate courier exists
        DO.Courier? dalCourier;
        try
        {
            dalCourier = s_dal.Courier.Read(courierId);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Courier {courierId} not found", ex);
        }

        if (dalCourier is null)
            throw new BlDoesNotExistException($"Courier {courierId} not found");

        // Prevent assignment to inactive courier
        if (!dalCourier.Active)
            throw new BlInvalidOperationException($"Courier {courierId} is not active and cannot be assigned deliveries.");

        // Prevent multiple active deliveries for same courier
        var activeDelivery = s_dal.Delivery.ReadAll(d => d.CourierId == courierId && d.EndOfOrder == null).FirstOrDefault();
        if (activeDelivery != null)
            throw new BlInvalidOperationException($"Courier {courierId} already has an active delivery (id {activeDelivery.Id}).");

        // Create delivery (keep existing pattern)
        DO.Delivery delivery = new DO.Delivery()
        {
            OrderId = orderId,
            CourierId = courierId,
            StartOfDelivery = DateTime.Now,
            TimeOfDelivery = null,
            EndOfOrder = null // In-progress
        };

        s_dal.Delivery.Create(delivery);

        // Try to find the created delivery to get its assigned Id (DAL Create doesn't return it)
        DO.Delivery? created = s_dal.Delivery.Read(d => d.OrderId == orderId && d.CourierId == courierId && d.EndOfOrder == null);
        if (created is null)
        {
            // Fallback: still notify order and courier so PL can refresh (local UI will re-read)
            Observers.NotifyListUpdated();
            try { OrderManager.Observers.NotifyItemUpdated(orderId); } catch { }
            try { CourierManager.Observers.NotifyItemUpdated(courierId); } catch { }
            return;
        }

        // Notify Delivery observers (by delivery id), Order observers (by order id) and Courier observers (by courier id)
        Observers.NotifyListUpdated();
        Observers.NotifyItemUpdated(created.Id);
        try { OrderManager.Observers.NotifyItemUpdated(orderId); } catch { }
        try { CourierManager.Observers.NotifyItemUpdated(courierId); } catch { }
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

        // Try to find the created delivery to get its Id
        var created = s_dal.Delivery.Read(d => d.OrderId == orderId && d.EndOfOrder == DO.EndOfOrder.Canceled && d.TimeOfDelivery.HasValue);

        Observers.NotifyListUpdated();
        if (created is not null)
        {
            // notify delivery observers by delivery id
            Observers.NotifyItemUpdated(created.Id);

            // notify order observers by order id so order-focused PL updates
            try { OrderManager.Observers.NotifyItemUpdated(orderId); } catch { }

            // no courier to notify (courierId == 0 in mock)
        }
        else
        {
            // fallback: notify order observers so PL can re-read the order
            try { OrderManager.Observers.NotifyItemUpdated(orderId); } catch { }
        }
    }

    internal static void CancelActiveDelivery(int orderId)
    {
        DO.Delivery delivery = s_dal.Delivery.Read(d => d.OrderId == orderId && d.EndOfOrder == null);
        if (delivery == null) return;

        DO.Delivery updated = delivery with
        {
            EndOfOrder = DO.EndOfOrder.Canceled,
            TimeOfDelivery = DateTime.Now
        };

        s_dal.Delivery.Update(updated);

        Observers.NotifyListUpdated();
        Observers.NotifyItemUpdated(updated.Id);

        // notify order observers and courier observers
        try { OrderManager.Observers.NotifyItemUpdated(updated.OrderId); } catch { }
        try { if (updated.CourierId != 0) CourierManager.Observers.NotifyItemUpdated(updated.CourierId); } catch { }
    }
}