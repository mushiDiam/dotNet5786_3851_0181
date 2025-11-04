namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;
internal class DeliveryImplementation : IDelivery
{
    public void Create(Delivery item)
    {
        if (DataSource.Deliveries.Exists(d => d.Id == item.Id))
            throw new NotImplementedException("An object of type 'Delivery' with this Id already exists");
        int id = Config.NextDeliveryId;
        Delivery copy = item with { Id = id };
        DataSource.Deliveries.Add(copy);
    }

    public void Delete(int id)
    {
        if(DataSource.Deliveries.Exists(d => d.Id == id))
        {
            DataSource.Deliveries.RemoveAll(d => d.Id == id);
            return;
        }
        throw new NotImplementedException("An object of type 'Delivery' with this Id doesn't exist");
    }

    public void DeleteAll()
    {
        DataSource.Deliveries.Clear();
    }

    public Delivery? Read(int id)
    {
        if(DataSource.Deliveries.Exists(d => d.Id == id))
        {
            return DataSource.Deliveries.Find(d => d.Id == id);
        }
        return null;
    }

    public List<Delivery> ReadAll()
    {
        return new List<Delivery>(DataSource.Deliveries);
    }

    public void Update(Delivery item)
    {
        Delete(item.Id);
        DataSource.Deliveries.Add(item);
    }
}
