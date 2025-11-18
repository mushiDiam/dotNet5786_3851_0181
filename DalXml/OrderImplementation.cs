namespace Dal;
using DalApi;
using DO;
using System;
using System.Collections.Generic;
using System.Linq;

internal class OrderImplementation : IOrder
{

    public void Create(Order item){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        int id = Config.NextOrderId;
        Order copy = item with { Id = id };
        Orders.Add(copy);
        XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
    }

    public void Delete(int id){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        if (Orders.Exists(o => o.Id == id)){
            Orders.RemoveAll(o => o.Id == id);
            XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
            return;
        }
        throw new DalDoesNotExistException($"Order with ID= {id} doesn't exists");
    }

    public void DeleteAll(){
        XMLTools.SaveListToXMLSerializer<Order>(new List<Order>(), Config.s_orders_xml);
    }

    public Order? Read(int id){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return Orders.FirstOrDefault(item => item.Id == id);
    }

    public Order? Read(Func<Order, bool> filter){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return Orders.FirstOrDefault(filter);

    }

    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml) ?? new List<Order>();
        return filter == null ? Orders : Orders.Where(filter);
    }

    public void Update(Order item){
        List<Order> Orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        Delete(item.Id);
        Orders.Add(item);
        XMLTools.SaveListToXMLSerializer<Order>(Orders, Config.s_orders_xml);
    }
}
