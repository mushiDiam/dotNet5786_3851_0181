namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;
internal class OrderImplementation : IOrder
{
    public void Create(Order item)
    {

        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public List<Order> ReadAll()
    {
        throw new NotImplementedException();
    }

    public void Update(Order item)
    {
        throw new NotImplementedException();
    }
}
