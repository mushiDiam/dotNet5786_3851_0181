namespace BlImplementation;

using System;
using System.Collections.Generic;
using BlApi;
using BlImplementation;
using BO;
using DalApi;
using DO;
using Helpers;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Collections.Specialized;

internal class OrderImplementation : BlApi.IOrder
{
    public async Task Add(int id, BO.Order order)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admin can add orders");
        await Task.Run(() => OrderManager.CreateOrder(order));
    }

    public void Cancel(int id, int orderId)
    {
        try
        {
            OrderManager.CancelOrder(orderId);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Order doesn't exist", ex);
        }
    }

    public void ChooseOrder(int id, int courierId, int orderId)
    {
        // 1. Authorization: Ensure the requester is the courier taking the job
        // (Or allow Admin, but instructions imply courier self-selection)
        if (id != courierId)
            throw new BlUnauthorizedAccessException("Only the specific courier can choose an order for themselves");

        // 2. Ensure courier exists and is active
        BO.Courier courier;
        try
        {
            courier = CourierManager.Read(courierId);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier doesn't exist", ex);
        }

        if (!courier.IsActive)
            throw new BlInvalidOperationException("Courier is not active and cannot pick up orders");

        // 3. Get the Order
        BO.Order? order = OrderManager.Read(orderId);
        if (order == null)
            throw new BlDoesNotExistException("Order doesn't exist");

        // 4. CRITICAL: Check that the order is strictly OPEN
        if (order.OrderStatus != OrderStatus.Open)
            throw new BlInvalidOperationException("Cannot choose an order that is not Open (it might be Shipped, Delivered, or Cancelled)");

        // 5. Create the delivery (DeliveryManager will also re-check courier state)
        DeliveryManager.AssignDelivery(orderId, courierId);
    }

    public int[] CountByType(int id)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get order summary");
        List<BO.Order> orders = OrderManager.ReadAll().ToList();
        if (orders.Count == 0)
        {
            throw new BlEmptyListException("No orders in the system");
        }
        int[] result = new int[Enum.GetValues(typeof(BO.ScheduleStatus)).Length + Enum.GetValues(typeof(BO.OrderStatus)).Length];
        Array schedules = Enum.GetValues(typeof(BO.ScheduleStatus));
        Array status = Enum.GetValues(typeof(BO.OrderStatus));
        for (int i = 0; i < Enum.GetValues(typeof(BO.ScheduleStatus)).Length; i++)
        {
            ScheduleStatus current = (ScheduleStatus)schedules.GetValue(i);
            var ordersByCurrentStatus = orders.GroupBy<BO.Order, BO.ScheduleStatus>(o => o.ScheduleStatus);
            int count = ordersByCurrentStatus.Count();
            result[i] = count;
        }
        for (int i = 0; i < Enum.GetValues(typeof(BO.OrderStatus)).Length; i++)
        {
            OrderStatus current = (OrderStatus)status.GetValue(i);
            var ordersByCurrentStatus = orders.GroupBy<BO.Order, BO.OrderStatus>(o => o.OrderStatus);
            int count = ordersByCurrentStatus.Count();
            result[i + Enum.GetValues(typeof(ScheduleStatus)).Length - 1] = count;
        }
        return result;
    }

    public void Delete(int id, int orderId)
    {
        throw new BlInvalidOperationException("Cannot delete an order");
    }

    public BO.Order? Details(int id, int orderId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get orders");
        return OrderManager.Read(orderId);
    }

    public void EndOfOrder(int id, int courierId, int deliveryId)
    {
        if (id != courierId)
            throw new BlUnauthorizedAccessException("Only couriers can only end their own deliveries");
        OrderManager.EndDelivery(courierId, deliveryId);
    }

   
    public IEnumerable<ClosedDeliveryInList> GetEndedDeliveries(int id, int courierId, OrderTypes? filter, OrderInListOptions? sort)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get orders");

        // Read all orders
        var orders = OrderManager.ReadAll()?.ToList() ?? new List<BO.Order>();

        // Project closed deliveries for the given courier
        var projected = orders
            .Where(o => o.Deliveries != null) // skip orders without deliveries
            .SelectMany(o =>
                o.Deliveries!
                .Where(d => d.CourierId == courierId && d.EndTime.HasValue)
                .Select(d => new
                {
                    Item = new BO.ClosedDeliveryInList
                    {
                        // map properties
                        DeliveryId = d.DeliveryId,
                        OrderId = o.Id,
                        OrderType = o.OrderType,
                        FullAddress = o.FullAddress,
                        Transport = d.Transport,
                        ActualDistance = o.AirDistance,
                        CompletionTime = d.EndTime.Value - d.StartTime,
                        OrderStatus = d.OrderStatus
                    },
                    // auxiliary fields for sorting
                    Schedule = o.ScheduleStatus,
                    AirDistance = o.AirDistance
                })
            );

        // Apply filter by OrderTypes if provided
        if (filter.HasValue)
        {
            projected = projected.Where(x => x.Item.OrderType == filter.Value);
        }

        // Sort according to sort parameter or by ScheduleStatus when null
        IOrderedEnumerable<dynamic> ordered;
        if (!sort.HasValue)
        {
            ordered = projected.OrderBy(x => x.Schedule).ThenBy(x => x.Item.DeliveryId);
        }
        else
        {
            switch (sort.Value)
            {
                case OrderInListOptions.DeliveryId:
                    ordered = projected.OrderBy(x => x.Item.DeliveryId);
                    break;
                case OrderInListOptions.OrderId:
                    ordered = projected.OrderBy(x => x.Item.OrderId);
                    break;
                case OrderInListOptions.OrderType:
                    ordered = projected.OrderBy(x => x.Item.OrderType);
                    break;
                case OrderInListOptions.AirDistance:
                    ordered = projected.OrderBy(x => x.AirDistance).ThenBy(x => x.Item.DeliveryId);
                    break;
                case OrderInListOptions.OrderStatus:
                    // OrderStatus is nullable; place nulls last
                    ordered = projected.OrderBy(x => x.Item.OrderStatus.HasValue ? 0 : 1)
                                       .ThenBy(x => x.Item.OrderStatus).ThenBy(x => x.Item.DeliveryId);
                    break;
                case OrderInListOptions.DeliveryCount:
                    // DeliveryCount not applicable for single delivery rows; fallback to DeliveryId
                    ordered = projected.OrderBy(x => x.Item.DeliveryId);
                    break;
                default:
                    ordered = projected.OrderBy(x => x.Schedule).ThenBy(x => x.Item.DeliveryId);
                    break;
            }
        }

        // Materialize a strongly-typed list instead of casting List<object> to IEnumerable<ClosedDeliveryInList>
        return ordered.Select(x => (BO.ClosedDeliveryInList)x.Item).ToList();
    }

    public IEnumerable<OpenOrderInList> GetOpenOrder(int id, int courierId, OrderTypes? filter, OrderInListOptions? sort)
    {
        // בדיקת הרשאות
        if (!AdminManager.IsAdmin(id) && id != courierId)
            throw new BlUnauthorizedAccessException("Only an admin or the courier themselves can get open orders");

        BO.Courier courier = CourierManager.Read(courierId);
        if (courier is null)
            throw new BlDoesNotExistException($"Courier {courierId} not found");

        // 1. שליפת כל ההזמנות
        var allOrders = OrderManager.ReadAll() ?? new List<BO.Order>();

        Debug.WriteLine("count of  orders within courier range: " + allOrders.Count());


        // 2. סינון קריטי: רק הזמנות בסטטוס Open
        // (מכיוון שאין CourierId ב-BO.Order, אנחנו מסתמכים על הסטטוס בלבד)


        var orderListToRet = from order in allOrders
                             let distance = order.AirDistance
                             where (OrderManager.CalculateOrderStatus(order.Id) == BO.OrderStatus.Open && distance <= courier.MaxDistancePreference)
                             
                             select order; //BuildOpenOrder(id, order);

        Debug.WriteLine("count of open orders within courier range: " + orderListToRet.Count());
        

        var openOrders = orderListToRet;

        // 3. סינון לפי סוג הזמנה
        if (filter.HasValue)
        {
            openOrders = openOrders.Where(o => o.OrderType == filter.Value);
        }

        // 5. המרה (Projection)
        var projected = openOrders.Select(o => new OpenOrderInList
        {
            CourierId = courierId,
            OrderId = o.Id,
            OrderType = o.OrderType,
            weight = o.Weight,
            volume = o.Volume,
            fragile = o.Fragile,
            FullAddress = o.FullAddress,
            AirDistance = o.AirDistance,

            // מונע תקיעה: לא מחשבים מסלול מלא dentro הרשימה
            ActualDistance = null,

            EstimatedTime = o.ExpectedDeliveryTime.HasValue ? o.ExpectedDeliveryTime.Value - DateTime.Now : null,
            ScheduleStatus = o.ScheduleStatus,
            RemainingTime = o.RemainingTime,
            MaxDeliveryTime = o.MaxDeliveryTime
        });

        // 6. מיון
        IOrderedEnumerable<OpenOrderInList> ordered;
        if (sort.HasValue)
        {
            ordered = sort.Value switch
            {
                OrderInListOptions.DeliveryId => projected.OrderBy(o => o.OrderId),
                OrderInListOptions.OrderId => projected.OrderBy(o => o.OrderId),
                OrderInListOptions.OrderType => projected.OrderBy(o => o.OrderType).ThenBy(o => o.OrderId),
                OrderInListOptions.AirDistance => projected.OrderBy(o => o.AirDistance).ThenBy(o => o.OrderId),
                _ => projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId)
            };
        }
        else
        {
            ordered = projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId);
        }

        Debug.WriteLine("Open Orders for Courier " + courierId + ":");

        foreach (var order in ordered)
        {
      
            Debug.WriteLine($"Open Order: {order.OrderId}, Distance: {order.AirDistance}");
        }


        return ordered.ToList();
    }
    public IEnumerable<OrderInList> GetOrders(int id, OrderInListOptions? filter, object? obj, OrderInListOptions? sort)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admin can get orders list");
        return OrderManager.GetOrders(filter, obj, sort);
    }

    public async Task UpdateDetails(int id, BO.Order order)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get orders");
        await OrderManager.Update(order);
    }
    // In OrderImplementation.cs
    public void MarkDeliveryNotFound(int requesterId, int courierId, int deliveryId)
    {
        // Authorization check
        if (requesterId != courierId)
            throw new BlUnauthorizedAccessException("Only the courier can mark their own delivery as not found");

        // Delegate to OrderManager
        OrderManager.MarkDeliveryNotFound(courierId, deliveryId);
    }

    #region Stage 5
    public void AddObserver(Action listObserver) =>
        OrderManager.Observers.AddListObserver(listObserver); //stage 5
    public void AddObserver(int id, Action observer) =>
        OrderManager.Observers.AddObserver(id, observer); //stage 5
    public void RemoveObserver(Action listObserver) =>
        OrderManager.Observers.RemoveListObserver(listObserver); //stage 5
    public void RemoveObserver(int id, Action observer) =>
        OrderManager.Observers.RemoveObserver(id, observer); //stage 5
    #endregion Stage 5
}
