namespace Dal;
using DalApi;
using DO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
internal class DeliveryImplementation : IDelivery
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Delivery item)
    {
        int id = Config.NextDeliveryId;
        Delivery copy = item with { Id = id };
        DataSource.Deliveries.Add(copy);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        if(DataSource.Deliveries.Exists(d => d.Id == id))
        {
            DataSource.Deliveries.RemoveAll(d => d.Id == id);
            return;
        }
        throw new DalDoesNotExistException($"Delivery with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Deliveries.Clear();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(int id)
    {
        return DataSource.Deliveries.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(Func<Delivery, bool> filter)
    {
        return DataSource.Deliveries.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null) //stage 2
       => filter == null
           ? DataSource.Deliveries.Select(item => item)
           : DataSource.Deliveries.Where(filter);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Delivery item)
    {
        Delete(item.Id);
        DataSource.Deliveries.Add(item);
    }
}
