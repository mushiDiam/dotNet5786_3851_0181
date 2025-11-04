namespace DalApi;
using DO;


public interface IConfig
{
    DateTime Clock { get; set; }
    double? MaxDeliveryDistance { get;}

    void Reset();
}
