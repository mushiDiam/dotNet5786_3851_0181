namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;
internal class OrderImplementation : IOrder
{
    public void Create(Order item)
    {
        int id = Config.NextOrderId;
        Order copy = item with { Id = id };
        DataSource.Orders.Add(copy);
    }

    public void Delete(int id)
    {
        if (DataSource.Orders.Exists(o => o.Id == id))
            DataSource.Orders.RemoveAll(o => o.Id == id);
        else
            throw new DalDoesNotExistException($"Order with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }

    public Order? Read(int id)
    {
        return DataSource.Orders.FirstOrDefault(item => item.Id == id);
    }

    public Order? Read(Func<Order, bool> filter)
    {
        return DataSource.Orders.FirstOrDefault(filter);
    }

    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null) //stage 2
        => filter == null
            ? DataSource.Orders.Select(item => item)
            : DataSource.Orders.Where(filter);


    public void Update(Order item)
    {
        Delete(item.Id);
        DataSource.Orders.Add(item);
    }
}