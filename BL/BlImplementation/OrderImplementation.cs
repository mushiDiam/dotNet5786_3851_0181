namespace BlImplementation;

using System;
using System.Collections.Generic;
using BlApi;
using BlImplementation;
using BO;
using DalApi;
using DO;
using Helpers;

internal class OrderImplementation : BlApi.IOrder
{
    public void Add(int id, BO.Order order)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admin can add orders");
        OrderManager.CreateOrder(order);
    }

    public void Cancel(int id, int orderId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admin can add orders");
        OrderManager.CancelOrder(orderId);
    }

    public void ChooseOrder(int id, int courierId, int orderId)
    {
        // 1. Authorization: Ensure the requester is the courier taking the job
        // (Or allow Admin, but instructions imply courier self-selection)
        if (id != courierId)
            throw new BlUnauthorizedAccessException("Only the specific courier can choose an order for themselves");

        // 2. Get the Order
        BO.Order? order = OrderManager.Read(orderId);
        if (order == null)
            throw new BlDoesNotExistException("Order doesn't exist");

        // 3. CRITICAL: Check that the order is strictly OPEN
        if (order.OrderStatus != OrderStatus.Open)
            throw new BlInvalidOperationException("Cannot choose an order that is not Open (it might be Shipped, Delivered, or Cancelled)");

        // 4. Create the delivery
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
       if(id != courierId)
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
                        DeliveryType = d.DeliveryType,
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

        // Return the mapped and ordered items
        return (IEnumerable<ClosedDeliveryInList>)ordered.Select(x => x.Item).ToList();
    }

    public IEnumerable<OpenOrderInList> GetOpenOrder(int id, int courierId, OrderTypes? filter, OrderInListOptions? sort)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get open orders");

        BO.Courier courier = CourierManager.Read(courierId);
        if (courier is null)
            throw new BlDoesNotExistException($"Courier {courierId} not found");

        // 3. Get all orders (BO.Order) and filter open
        List<BO.Order> allOrders = OrderManager.ReadAll()?.Where(o => o.OrderStatus == OrderStatus.Open).ToList()
                                  ?? new List<BO.Order>();

        // 4. Filter orders by type if requested
        if (filter.HasValue)
            allOrders = allOrders.Where(o => o.OrderType == filter.Value).ToList();

        // 5. Filter orders by courier distance
        allOrders = allOrders
            .Where(o => o.AirDistance <= courier.MaxDistancePreference)
            .ToList();

        // 6. Map to OpenOrderInList
        var projected = allOrders
            .Select(o => new OpenOrderInList
            {
                CourierId = courierId,
                OrderId = o.Id,
                OrderType = o.OrderType,
                weight = o.Weight,
                volume = o.Volume,
                fragile = o.Fragile,
                FullAddress = o.FullAddress,
                AirDistance = o.AirDistance,
                ActualDistance = OrderManager.GetActualDistanceAsync(o.Latitude, o.Longitude, courier.Transport).GetAwaiter().GetResult(),
                EstimatedTime = o.ExpectedDeliveryTime.HasValue ? o.ExpectedDeliveryTime.Value - DateTime.Now : null,
                ScheduleStatus = o.ScheduleStatus, // 1:1 copy
                RemainingTime = o.RemainingTime,
                MaxDeliveryTime = o.MaxDeliveryTime
            });

        // 7. Apply sorting
        IOrderedEnumerable<OpenOrderInList> ordered;
        if (sort.HasValue)
        {
            ordered = sort.Value switch
            {
                OrderInListOptions.DeliveryId => projected.OrderBy(o => o.OrderId), // fallback
                OrderInListOptions.OrderId => projected.OrderBy(o => o.OrderId),
                OrderInListOptions.OrderType => projected.OrderBy(o => o.OrderType).ThenBy(o => o.OrderId),
                OrderInListOptions.AirDistance => projected.OrderBy(o => o.AirDistance).ThenBy(o => o.OrderId),
                OrderInListOptions.OrderStatus => projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId),
                OrderInListOptions.ScheduleStatus => projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId),
                OrderInListOptions.RemainingTime => projected.OrderBy(o => o.RemainingTime).ThenBy(o => o.OrderId),
                OrderInListOptions.CompletionTime => projected.OrderBy(o => o.MaxDeliveryTime).ThenBy(o => o.OrderId),
                OrderInListOptions.DeliveryCount => projected.OrderBy(o => o.OrderId),
                _ => projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId)
            };
        }
        else
        {
            // default sort = ScheduleStatus, then OrderId
            ordered = projected.OrderBy(o => o.ScheduleStatus).ThenBy(o => o.OrderId);
        }

        return ordered.ToList();
    }
    public IEnumerable<OrderInList> GetOrders(int id, OrderInListOptions? filter, object? obj, OrderInListOptions? sort)
    {
       if(!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admin can get orders list");
       return OrderManager.GetOrders(filter, obj, sort);
    }

    public void UpdateDetails(int id, BO.Order order)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can get orders");
        OrderManager.Update(order);
    }
}
