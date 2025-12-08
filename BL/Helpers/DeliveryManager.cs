using DalApi;
using DO;
namespace Helpers;

internal static class DeliveryManager{
    private static IDal s_dal = Factory.Get;
    public static List<BO.Order> GetAllOrdersByCourier(int courierId)
    {
        var deliveries = s_dal.Delivery.ReadAll(d => d.CourierId == courierId);
        List<BO.Order> orders = new List<BO.Order>();
        foreach (var delivery in deliveries)
        {
            var doOrder = s_dal.Order.Read(o => o.Id == delivery.OrderId);
            BO.Order boOrders = OrderManager.ConvertToBO(doOrder);
            orders.Add(boOrders);
        }
        return orders;
    }
    public static Delivery GetDelivery(int orderId)
    { 
        return s_dal.Delivery.Read(d => d.OrderId == orderId); ;
    }
}
