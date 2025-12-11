namespace BLImplementation;
using BlApi;
using BO;
using System.Collections.Generic;

internal class OrderImplementation : IOrder
{
    public void Add(int id, Order O)
    {
        throw new NotImplementedException();
    }

    public void Cancel(int id, int orderId)
    {
        throw new NotImplementedException();
    }

    public void ChooseOrder(int id, int courierId, int orderId)
    {
        throw new NotImplementedException();
    }

    public int[] CountByType(int id)
    {
        throw new NotImplementedException();
    }

    public void Delete(int id, int orderId)
    {
        throw new NotImplementedException();
    }

    public Order Details(int id, int orderId)
    {
        throw new NotImplementedException();
    }

    public void EndOfOrder(int id, int courierId, int orderId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ClosedDeliveryInList> GetEndedDeliveries(int id, int courierI, OrderTypes? filter, OrderOptions? sort)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<OpenOrderInList> GetOpenOrder(int id, int courierI, OrderTypes? filter, OrderOptions? sort)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<OrderInList> GetOrders(int id, OrderOptions? filter, object? obj, OrderOptions? sort)
    {
        throw new NotImplementedException();
    }

    public void UpdateDetails(int id, Order O)
    {
        throw new NotImplementedException();
    }
}
