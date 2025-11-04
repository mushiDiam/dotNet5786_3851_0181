using DO;

namespace Dal;

public record Courier
(
    int Id,
    bool Active,
    double? MinDeliveryDistance,
    DateTime DeliveryTime,
    OrderType OrderType,
    string Name = "",
    string Phone = "",
    string Email = "",
    string Password = "",
    string VehicleType = ""
)
{ }