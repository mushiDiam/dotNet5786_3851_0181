namespace Dal;
using DalApi;
using DO;
using System;
using System.Collections.Generic;

internal class DeliveryImplementation : IDelivery
{
    public void Create(Delivery item)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        int id = Config.NextDeliveryId;
        Delivery copy = item with { Id = id };
        Deliveries.Add(copy);
    }

    public void Delete(int id)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        if (Deliveries.Exists(d => d.Id == id))
        {
            Deliveries.RemoveAll(d => d.Id == id);
            return;
        }
        throw new DalDoesNotExistException($"Delivery with ID= {id} doesn't exists");
    }

    public void DeleteAll()
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        Deliveries.Clear();
    }

    public Delivery? Read(int id)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return Deliveries.FirstOrDefault(item => item.Id == id);

    }

    public Delivery? Read(Func<Delivery, bool> filter)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return Deliveries.FirstOrDefault(filter);

    }

    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return filter == null ? Deliveries : Deliveries.Where(filter);

    }

    public void Update(Delivery item)
    {
        List<Delivery> Deliveries = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        Delete(item.Id);
        Deliveries.Add(item);
    }
}

