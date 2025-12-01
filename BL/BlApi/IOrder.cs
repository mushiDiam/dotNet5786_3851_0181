using BO;

namespace BlApi;

public interface IOrder{
    int[] CountByType(int id);
    IEnumerable<BO.OrderInList> GetOrders(int id, OrderOptions? filter,object? obj , OrderOptions? sort);
    BO.Order Details(int id, int orderId);
    void UpdateDetails(int id, BO.Order O);
    void Cancel(int id, int orderId);
    void Delete(int id, int orderId);
    void Add(int id, BO.Order O);
    void EndOfOrder(int id,int courierId, int orderId);
    void ChooseOrder(int id, int courierId, int orderId);
    IEnumerable<BO.ClosedDeliveryInList> GetEndedDeliveries(int id, int courierI, OrderTypes? filter, OrderOptions? sort );
    IEnumerable<BO.OpenOrderInList> GetOpenOrder(int id, int courierI, OrderTypes? filter, OrderOptions? sort);
}
