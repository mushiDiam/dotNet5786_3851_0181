namespace BlApi;

public interface IBI{
    IAdmin Admin { get; }
    ICourier Courier { get; }
    IOrder Order { get; }
}
