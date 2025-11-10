namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;
internal class DeliveryImplementation : IDelivery
{
    public void Create(Delivery item)
    {
        if (DataSource.Deliveries.Exists(d => d.Id == item.Id))
            throw new Exception($"Delivery with ID= {item.Id} already exists");
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
        throw new NotImplementedException($"Delivery with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        DataSource.Deliveries.Clear();
    }

    public Delivery? Read(int id)
    {
        return DataSource.Deliveries.FirstOrDefault(item => item.Id == id);
    }

    public Delivery? Read(Func<Delivery, bool> filter)
    {
        return DataSource.Deliveries.FirstOrDefault(filter);
    }

    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null) //stage 2
       => filter == null
           ? DataSource.Deliveries.Select(item => item)
           : DataSource.Deliveries.Where(filter);

    public void Update(Delivery item)
    {
        Delete(item.Id);
        DataSource.Deliveries.Add(item);
    }
}
