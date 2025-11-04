namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;
internal class OrderImplementation : IOrder
{
    public void Create(Order item)
    {
        if(DataSource.Orders.Exists(o => o.Id == item.Id))
            throw new NotImplementedException("An object of type 'Order' with this Id already exists");
        int id = Config.NextOrderId;
        Order copy = item with { Id = id };
        DataSource.Orders.Add(copy);
    }

    public void Delete(int id)
    {
        if(DataSource.Orders.Exists(o => o.Id == id))
            DataSource.Orders.RemoveAll(o => o.Id == id);
        else
            throw new NotImplementedException("An object of type 'Order' with this ID doesn't exists");
    }

    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }

    public Order? Read(int id)
    {
        if(DataSource.Orders.Exists(o => o.Id == id))
        {
            return DataSource.Orders.Find(o => o.Id == id);
        }
        return null;
    }

    public List<Order> ReadAll()
    {
        return new List<Order>(DataSource.Orders);
    }

    public void Update(Order item)
    {
        if(DataSource.Orders.Exists(o => o.Id == item.Id))
        {
            Delete(item.Id);
            Create(item);
            return;
        }
        throw new NotImplementedException("An object of type 'Order' with this Id doesn't exist");
    }
}
