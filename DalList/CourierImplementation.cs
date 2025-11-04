namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;

public class CourierImplementation : ICourier
{
    public void Create(Courier item)
    {
        if (DataSource.Couriers.Exists(c => c.Id == item.Id))
            throw new Exception($"Courier with ID= {item.Id} already exists");
        else
            DataSource.Couriers.Add(item);
    }

    public void Delete(int id)
    {
        if (DataSource.Couriers.Exists(c => c.Id == id))
            DataSource.Couriers.RemoveAll(c => c.Id == id);
        else
            throw new Exception($"Courier with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }

    public Courier? Read(int id)
    {
        if (DataSource.Couriers.Exists(c => c.Id == id))
            return DataSource.Couriers.Find(c => c.Id == id);
        else
            return null;
    }

    public List<Courier> ReadAll()
    {
        return new List<Courier>(DataSource.Couriers);
    }

    public void Update(Courier item)
    {
        Delete(item.Id);
        DataSource.Couriers.Add(item);
    }
}