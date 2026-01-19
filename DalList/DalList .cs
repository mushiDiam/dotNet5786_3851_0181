namespace Dal;

using System.Data.SqlTypes;
using DalApi;
sealed internal class DalList : IDal
{
    private DalList() { }
    private static readonly Lazy<IDal> _instance = new(() => new DalList());
    public static IDal Instance => _instance.Value;
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
