namespace Dal;

using System.Collections.Generic;
using DalApi;
using DO;

internal class CourierImplementation : ICourier
{
    public void Create(Courier item)
    {
        if (DataSource.Couriers.Exists(c => c.Id == item.Id))
            throw new DalAlreadyExistsException($"Courier with ID= {item.Id} already exists");
        else
            DataSource.Couriers.Add(item);
    }

    public void Delete(int id)
    {
        if (DataSource.Couriers.Exists(c => c.Id == id))
            DataSource.Couriers.RemoveAll(c => c.Id == id);
        else
            throw new DalDoesNotExistException($"Courier with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }

    public Courier? Read(int id)
    {
        return DataSource.Couriers.FirstOrDefault(item => item.Id == id);
    }

    public Courier? Read(Func<Courier, bool> filter)
    {
        return DataSource.Couriers.FirstOrDefault(filter);
    }

    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null) //stage 2
         => filter == null
             ? DataSource.Couriers.Select(item => item)
             : DataSource.Couriers.Where(filter);

    public void Update(Courier item)
    {
        Delete(item.Id);
        DataSource.Couriers.Add(item);
    }
}