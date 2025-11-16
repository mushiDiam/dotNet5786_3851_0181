namespace Dal;
using DalApi;
using DO;
using System;
using System.Collections.Generic;

internal class CourierImplementation : ICourier
{
    public void Create(Courier item)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        if (Couriers.Exists(c => c.Id == item.Id))
            throw new DalAlreadyExistsException($"Courier with ID= {item.Id} already exists");
        else
            Couriers.Add(item);
    }

    public void Delete(int id)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        if (Couriers.Exists(c => c.Id == id))
            Couriers.RemoveAll(c => c.Id == id);
        else
            throw new DalDoesNotExistException($"Courier with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        Couriers.Clear();
    }

    public Courier? Read(int id)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        return Couriers.FirstOrDefault(item => item.Id == id);

    }

    public Courier? Read(Func<Courier, bool> filter)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        return Couriers.FirstOrDefault(filter);

    }

    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        return filter == null ? Couriers : Couriers.Where(filter);
    }

    public void Update(Courier item)
    {
        List<Courier> Couriers = XMLTools.LoadListFromXMLSerializer<Courier>(Config.s_couriers_xml);
        Delete(item.Id);
        Couriers.Add(item);
    }
}

