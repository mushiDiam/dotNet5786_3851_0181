namespace BlApi;

public interface IBl{
    IAdmin Admin { get; }
    ICourier Courier { get; }
    IOrder Order { get; }
}
