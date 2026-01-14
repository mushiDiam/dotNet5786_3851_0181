namespace Dal;
using DalApi;
using DO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
internal class OrderImplementation : IOrder
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Order item)
    {
        int id = Config.NextOrderId;
        Order copy = item with { Id = id };
        DataSource.Orders.Add(copy);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        if (DataSource.Orders.Exists(o => o.Id == id))
            DataSource.Orders.RemoveAll(o => o.Id == id);
        else
            throw new DalDoesNotExistException($"Order with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(int id)
    {
        return DataSource.Orders.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(Func<Order, bool> filter)
    {
        return DataSource.Orders.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null) //stage 2
        => filter == null
            ? DataSource.Orders.Select(item => item)
            : DataSource.Orders.Where(filter);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Order item)
    {
        Delete(item.Id);
        DataSource.Orders.Add(item);
    }
}