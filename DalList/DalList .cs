namespace Dal;
using DalApi;
sealed internal class DalList : IDal
{
    private DalList() { }
    public static IDal Instance { get; } = new DalList();
    public IOrder Order { get; } = new OrderImplementation();

    public ICourier Courier { get; } = new CourierImplementation();

    public IDelivery Delivery { get; } = new DeliveryImplementation();

    public IConfig Config { get; } = new ConfigImplementation();

    public void resetDB(){
       Order.DeleteAll();
       Courier.DeleteAll();
       Delivery.DeleteAll();
       Config.Reset();
    }
   
}
