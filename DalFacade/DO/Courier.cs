namespace Dal;
using DO;

/// <summary>
/// Represents a courier used for deliveries.
/// </summary>
/// <param name="Id">Unique identifier for the courier.</param>
/// <param name="Active">Indicates whether the courier is currently active and available for assignments.</param>
/// <param name="MaxDeliveryDistance">
/// Maximum delivery distance (in kilometers) the courier is willing or able to travel.
/// A null value indicates no explicit distance limit.
/// </param>
/// <param name="DeliveryTime">Typical delivery time or timestamp associated with courier availability.</param>
/// <param name="OrderType">Preferred or supported order transport type (Car, Motorcycle, Bike, Walking).</param>
/// <param name="Name">Full name of the courier. Defaults to an empty string if not provided.</param>
/// <param name="Phone">Contact phone number for the courier. Defaults to an empty string if not provided.</param>
/// <param name="Email">Contact email for the courier. Defaults to an empty string if not provided.</param>
/// <param name="Password">Courier authentication password or password hash. Defaults to an empty string if not provided.
/// Note: store passwords securely (hashed + salted) in production systems; plain text is discouraged.</param>
public record Courier
(
    int Id,
    bool Active,
    double? MaxDeliveryDistance,
    DateTime DeliveryTime,
    OrderType OrderType,
    string Name = "",
    string Phone = "",
    string Email = "",
    string Password = ""
)
{ }