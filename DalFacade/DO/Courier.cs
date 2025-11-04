using DO;

namespace Dal;

public record Courier
(
    int Id,//Courier's Id , natural key for the courier.
    bool Active,//Is the courier active or not.
    double? MaxDeliveryDistance,//Maximum distance the courier is willing to deliver.
    DateTime DeliveryTime,//The time the delivery started.
    OrderType OrderType,//How the courier delivers the order.
    string Name = "",//Courier's name.
    string Phone = "",//Courier's phone number.
    string Email = "",//Courier's email.
    string Password = ""//Courier's password.
)
{ }