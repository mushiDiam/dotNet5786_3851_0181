namespace Dal;
using DalApi;
using DO;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class DeliveryImplementation : IDelivery
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Delivery item){
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        int id = Config.NextDeliveryId;
        Delivery copy = item with { Id = id };
        Deliverys.Add(copy);
        XMLTools.SaveListToXMLSerializer<Delivery>(Deliverys, Config.s_deliveries_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    // In DeliveryImplementation.cs
    public void Delete(int id)
    {
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);

        if (Deliverys.Exists(d => d.Id == id))
        {
            Deliverys.RemoveAll(d => d.Id == id);  // Remove ALL duplicates
            XMLTools.SaveListToXMLSerializer<Delivery>(Deliverys, Config.s_deliveries_xml);
            return;
        }
        throw new DalDoesNotExistException($"Delivery with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll(){
        XMLTools.SaveListToXMLSerializer<Delivery>(new List<Delivery>(), Config.s_deliveries_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(int id){
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return Deliverys.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(Func<Delivery, bool> filter){
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return Deliverys.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null){
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return filter == null ? Deliverys : Deliverys.Where(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Delivery item)
    {
        List<Delivery> Deliverys = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);

        // Remove ALL instances of this ID (to clean up duplicates)
        Deliverys.RemoveAll(d => d.Id == item.Id);

        // Add the updated delivery
        Deliverys.Add(item);

        // Save once
        XMLTools.SaveListToXMLSerializer<Delivery>(Deliverys, Config.s_deliveries_xml);
    }
}

