namespace Dal;
using DalApi;
using DO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

internal class OrderImplementation : IOrder
{

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Order item){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        int id = Config.NextOrderId;
        Order copy = item with { Id = id };
        Orders.Add(copy);
        XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        if (Orders.Exists(o => o.Id == id)){
            Orders.RemoveAll(o => o.Id == id);
            XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
            return;
        }
        throw new DalDoesNotExistException($"Order with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll(){
        XMLTools.SaveListToXMLSerializer<Order>(new List<Order>(), Config.s_orders_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(int id){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return Orders.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(Func<Order, bool> filter){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return Orders.FirstOrDefault(filter);

    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml) ?? new List<Order>();
        return filter == null ? Orders : Orders.Where(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Order item){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        Delete(item.Id);
        Orders.Add(item);
        XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
    }
}
