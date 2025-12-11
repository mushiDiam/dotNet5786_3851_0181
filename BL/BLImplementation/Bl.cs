namespace BLImplementation;
using BlApi;
internal class Bl : IBl
{
    public IAdmin Admin { get; } = new AdminImplementation();
    public ICourier Courier { get; } = new CourierImplementation();
    public IOrder Order { get; } = new OrderImplementation();
}
