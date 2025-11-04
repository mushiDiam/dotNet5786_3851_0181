namespace Dal;

internal static class Config
{
    // ------------------------------
    // Running Identifiers
    // ------------------------------

    /// <summary>
    /// Represents the next available order ID.
    /// Increments automatically by 1 when a new order is added.
    /// </summary>
    internal static int NextOrderId { get; private set; }

    /// <summary>
    /// Represents the next available delivery ID.
    /// Increments automatically by 1 when a new delivery entity is created.
    /// </summary>
    internal static int NextDeliveryId { get; private set; }

    // ------------------------------
    // System Clock
    // ------------------------------

    /// <summary>
    /// Virtual system clock maintained separately from the computer's real-time clock.
    /// Can be initialized or advanced by the manager or simulator.
    /// </summary>
    internal static DateTime Clock { get; set; }

    // ------------------------------
    // Manager (Admin)
    // ------------------------------

    /// <summary>
    /// Manager’s national ID number (validated with checksum).
    /// </summary>
    internal static int AdminId { get; set; }

    /// <summary>
    /// Manager’s password (initially assigned and can later be updated).
    /// Should be stored encrypted and validated for strength.
    /// </summary>
    internal static string AdminPassword { get; set; } = string.Empty;

    // ------------------------------
    // Company Address
    // ------------------------------

    /// <summary>
    /// Full and valid company address.
    /// Null if not yet initialized or invalid.
    /// Used as the start and end point for deliveries.
    /// </summary>
    internal static string? CompanyAddress { get; set; }

    /// <summary>
    /// Latitude coordinate of the company address.
    /// Automatically updated by the logic layer after address validation.
    /// Remains null if the address is invalid.
    /// </summary>
    internal static double? Latitude { get; private set; }

    /// <summary>
    /// Longitude coordinate of the company address.
    /// Automatically updated by the logic layer after address validation.
    /// Remains null if the address is invalid.
    /// </summary>
    internal static double? Longitude { get; private set; }

    // ------------------------------
    // Distance and Speed Settings
    // ------------------------------

    /// <summary>
    /// Maximum allowed aerial distance (in kilometers) between the company and an order’s address.
    /// If null, there is no delivery distance limit.
    /// </summary>
    internal static double? MaxDeliveryDistanceKm { get; set; }

    /// <summary>
    /// Average car travel speed in km/h.
    /// Used to calculate expected delivery times.
    /// </summary>
    internal static double AverageCarSpeedKmh { get; set; }

    /// <summary>
    /// Average motorcycle travel speed in km/h.
    /// Used for delivery time calculations.
    /// </summary>
    internal static double AverageMotorcycleSpeedKmh { get; set; }

    /// <summary>
    /// Average bicycle travel speed in km/h.
    /// Used for short-distance delivery calculations.
    /// </summary>
    internal static double AverageBikeSpeedKmh { get; set; }

    /// <summary>
    /// Average walking speed in km/h.
    /// Used for pedestrian delivery time calculations.
    /// </summary>
    internal static double AverageWalkingSpeedKmh { get; set; }

    // ------------------------------
    // Time Ranges
    // ------------------------------

    /// <summary>
    /// Maximum delivery time commitment for all orders.
    /// Helps determine on-time and delayed deliveries.
    /// </summary>
    internal static TimeSpan MaxDeliveryTimeRange { get; set; }

    /// <summary>
    /// Time range after which an order is considered “at risk”
    /// if it has not yet been delivered.
    /// </summary>
    internal static TimeSpan RiskRange { get; set; }

    /// <summary>
    /// Time range of inactivity for a courier,
    /// after which they are automatically marked as “inactive.”
    /// </summary>
    internal static TimeSpan InactivityTimeRange { get; set; }

    // ------------------------------
    // Helper Methods
    // ------------------------------

    /// <summary>
    /// Generates and returns the next order ID.
    /// </summary>
    internal static int GenerateNextOrderId()
    {
        return ++NextOrderId;
    }

    /// <summary>
    /// Generates and returns the next delivery ID.
    /// </summary>
    internal static int GenerateNextDeliveryId()
    {
        return ++NextDeliveryId;
    }

    /// <summary>
    /// Updates the geographic coordinates after address validation.
    /// </summary>
    internal static void UpdateCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
