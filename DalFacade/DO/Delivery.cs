using DalApi;

namespace Dal;

/// <summary>
/// Represents a delivery entity in the system,
/// linking an order to the courier who handles it.
/// </summary>
internal record Delivery
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