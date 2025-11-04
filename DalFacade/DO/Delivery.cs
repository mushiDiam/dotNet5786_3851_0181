using DalApi;

namespace Dal;

/// <summary>
/// Represents a delivery entity in the system,
/// linking an order to the courier who handles it.
/// </summary>
internal class Delivery
{
    // ------------------------------
    // Identification
    // ------------------------------

    /// <summary>
    /// Unique running delivery ID.
    /// Retrieved from Config when a new delivery is created.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// The ID of the order handled in this delivery.
    /// </summary>
    public int OrderId { get; private set; }

    /// <summary>
    /// The ID of the courier who accepted this delivery.
    /// In case of a "virtual delivery" (canceled order before assignment),
    /// this value will be 0.
    /// </summary>
    public int CourierId { get; private set; }

    // ------------------------------
    // Delivery Type
    // ------------------------------

    /// <summary>
    /// The type of delivery at creation time (e.g., Car, Bike, Walking, etc.).
    /// </summary>
    public DeliveryType Type { get; private set; }

    // ------------------------------
    // Timing
    // ------------------------------

    /// <summary>
    /// Date and time when the delivery started (the courier picked up the order).
    /// Set according to the current system clock in the Config entity.
    /// For virtual deliveries, the end time equals the start time.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Date and time when the delivery ended.
    /// Automatically set when the completion type is updated.
    /// Null if the delivery is still in progress.
    /// </summary>
    public DateTime? EndTime { get; private set; }

    // ------------------------------
    // Distance
    // ------------------------------

    /// <summary>
    /// The actual distance (in kilometers) between the company and the order’s address.
    /// Computed by the logic layer and stored once completed.
    /// Null until the distance calculation is finished.
    /// </summary>
    public double? ActualDistanceKm { get; private set; }

    // ------------------------------
    // Completion Type
    // ------------------------------

    /// <summary>
    /// The way this delivery was completed (e.g., Delivered, Failed, Canceled, etc.).
    /// Null if still in progress.
    /// </summary>
    public DeliveryCompletionType? CompletionType { get; private set; }

    // ------------------------------
    // Constructors
    // ------------------------------

    /// <summary>
    /// Default constructor for serialization or initialization.
    /// </summary>
    public Delivery() { }

    /// <summary>
    /// Creates a new delivery entity.
    /// Automatically retrieves the next delivery ID from Config.
    /// </summary>
    public Delivery(int orderId, int courierId, DeliveryType type)
    {
        Id = Config.GenerateNextDeliveryId();
        OrderId = orderId;
        CourierId = courierId;
        Type = type;
        StartTime = Config.Clock; // use the current system clock
    }

    // ------------------------------
    // Logic-layer update methods
    // ------------------------------

    /// <summary>
    /// Updates the actual distance once computed by the logic layer.
    /// </summary>
    public void SetActualDistance(double distanceKm)
    {
        ActualDistanceKm = distanceKm;
    }

    /// <summary>
    /// Sets the completion type and updates the end time accordingly
    /// based on the current system clock from Config.
    /// </summary>
    public void Complete(DeliveryCompletionType completionType)
    {
        CompletionType = completionType;
        EndTime = Config.Clock;
    }
}


/// <summary>
/// Represents the mode of delivery (transportation method).
/// </summary>


/// <summary>
/// Represents the result or outcome of a delivery.
/// </summary>
internal enum DeliveryCompletionType
{
    Delivered,
    Failed,
    Canceled
}

public enum DeliviryTransportation{
    Car,
    Motorcycle,
    Bike,
    Walking
}