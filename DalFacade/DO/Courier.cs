namespace Dal;

public enum DeliveryType
{
    Car,
    Motorcycle,
    Bike,
    Walking
}

public record Courier
{
    /// <summary>
    /// Courier ID - unique identifier.
    /// Numeric value with a valid check digit.
    /// </summary>
    public int Id;  // Cannot be updated

    /// <summary>
    /// Full name (first and last).
    /// </summary>
    public string FullName { get; set; }  // Can be updated by manager or courier

    /// <summary>
    /// Valid mobile phone number (10 digits, starts with 0).
    /// </summary>
    public string PhoneNumber { get; set; }  // Can be updated by manager or courier

    /// <summary>
    /// Valid email address (format checked at the logical layer).
    /// </summary>
    public string Email { get; set; }  // Can be updated by manager or courier

    /// <summary>
    /// Encrypted password (optional).
    /// Initially set by manager; can later be updated by courier.
    /// Password strength and encryption handled in the logic layer.
    /// </summary>
    public string? PasswordHash { get; set; }  // Optional feature

    /// <summary>
    /// Indicates whether the courier is active.
    /// Only a manager can change this.
    /// </summary>
    public bool IsActive { get; private set; } // Can be updated only by manager

    /// <summary>
    /// Maximum personal delivery distance (in km, for example).
    /// If null, there is no distance limit.
    /// Must be less than or equal to the company's global max distance.
    /// </summary>
    public double? MaxDeliveryDistanceKm { get; set; }  // Can be updated by manager or courier

    /// <summary>
    /// Delivery type: Vehicle, Motorcycle, Bicycle, or Walking.
    /// Can be updated as long as the courier is not currently handling an order.
    /// </summary>
    public DeliveryType DeliveryType { get; set; } // Can be updated by manager or courier

    /// <summary>
    /// The date and time the courier started working for the company.
    /// Automatically set when the courier entity is created.
    /// </summary>
    public DateTime StartDateTime;  // Set at creation, cannot be updated


    public override string ToString()
    {
        return $"{FullName} ({DeliveryType}) - Active: {IsActive}";
    }
}
