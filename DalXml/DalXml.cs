using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DalApi;

namespace Dal;

//stage 3
sealed internal class DalXml : IDal
{
    private DalXml() { }
    public static IDal Instance { get; } = new DalXml();
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
