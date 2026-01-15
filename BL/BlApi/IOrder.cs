using BO;

namespace BlApi;

public interface IOrder : IObservable
{
    int[] CountByType(int id);
    
    // Existing sync version (keep for backward compatibility)
    IEnumerable<BO.OrderInList> GetOrders(int id, OrderInListOptions? filter, object? obj, OrderInListOptions? sort);
    
    // NEW: Async version with network calls for real driving distance
    Task<IEnumerable<BO.OrderInList>> GetOrdersAsync(int id, OrderInListOptions? filter, object? obj, OrderInListOptions? sort);
    
    BO.Order? Details(int id, int orderId);
    Task UpdateDetails(int id, BO.Order O);
    void Cancel(int id, int orderId);
    void Delete(int id, int orderId);
    Task Add(int id, BO.Order O);
    void EndOfOrder(int id, int courierId, int orderId);
    void ChooseOrder(int id, int courierId, int orderId);
    IEnumerable<BO.ClosedDeliveryInList> GetEndedDeliveries(int id, int courierId, OrderTypes? filter, OrderInListOptions? sort);
    IEnumerable<BO.OpenOrderInList> GetOpenOrder(int id, int courierId, OrderTypes? filter, OrderInListOptions? sort);
    void MarkDeliveryNotFound(int requesterId, int courierId, int deliveryId);
}
