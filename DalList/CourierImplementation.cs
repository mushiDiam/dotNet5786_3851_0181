namespace Dal;
using DalApi;
using DO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class CourierImplementation : ICourier
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Courier item)
    {
        if (DataSource.Couriers.Exists(c => c.Id == item.Id))
            throw new DalAlreadyExistsException($"Courier with ID= {item.Id} already exists");
        else
            DataSource.Couriers.Add(item);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        if (DataSource.Couriers.Exists(c => c.Id == id))
            DataSource.Couriers.RemoveAll(c => c.Id == id);
        else
            throw new DalDoesNotExistException($"Courier with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(int id)
    {
        return DataSource.Couriers.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(Func<Courier, bool> filter)
    {
        return DataSource.Couriers.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null) //stage 2
         => filter == null
             ? DataSource.Couriers.Select(item => item)
             : DataSource.Couriers.Where(filter);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(DO.Courier courier)
    {
        DO.Courier? existing = DataSource.Couriers.FirstOrDefault(c => c.Id == courier.Id);
       
        if (existing is null)
        {
            throw new DO.DalDoesNotExistException($"Courier with ID {courier.Id} does not exist");
        }

        DataSource.Couriers.Remove(existing);
        DataSource.Couriers.Add(courier);
    }
}