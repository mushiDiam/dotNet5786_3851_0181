namespace Dal;

public record Courier
(
    int Id,
    bool Active,
    double? MinDeliveryDistance,
    DateTime DeliveryTime,
    string Name = "",
    string Phone = "",
    string Email = "",
    string Password = "",
    string VehicleType = ""
)
{ }