namespace Dal;

/// <summary>
/// Represents an order entity in the system.
/// </summary>
internal class Order
{
    // ------------------------------
    // Identification
    // ------------------------------

    /// <summary>
    /// Unique running order ID.
    /// Retrieved from the configuration entity when a new order is created.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// The type of the order (defined by an enum according to company type).
    /// </summary>
    public OrderType Type { get; set; }

    // ------------------------------
    // Description
    // ------------------------------

    /// <summary>
    /// Optional textual description of the order content.
    /// </summary>
    public string? Description { get; set; }

    // ------------------------------
    // Address
    // ------------------------------

    /// <summary>
    /// Full and valid address of the order destination.
    /// Must be a valid existing address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate of the order address.
    /// Automatically updated by the logic layer after address validation.
    /// </summary>
    public double Latitude { get; private set; }

    /// <summary>
    /// Longitude coordinate of the order address.
    /// Automatically updated by the logic layer after address validation.
    /// </summary>
    public double Longitude { get; private set; }

    // ------------------------------
    // Customer Information
    // ------------------------------

    /// <summary>
    /// Full name of the customer who placed the order.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's mobile phone number.
    /// Must be a valid 10-digit number starting with '0'.
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    // ------------------------------
    // Optional Attributes
    // ------------------------------

    /// <summary>
    /// Additional details such as volume, weight, fragility, etc.
    /// These are mandatory fields but their structure may vary depending on the company.
    /// </summary>
    public string? AdditionalDetails { get; set; }

    // ------------------------------
    // Timing
    // ------------------------------

    /// <summary>
    /// The date and time when the order was created (opened).
    /// Set according to the system clock value in the configuration entity.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // ------------------------------
    // Constructors
    // ------------------------------

    /// <summary>
    /// Default constructor for serialization or initialization.
    /// </summary>
    public Order() { }

    /// <summary>
    /// Creates a new order with all required fields.
    /// </summary>
    public Order(
        int id,
        OrderType type,
        string address,
        double latitude,
        double longitude,
        string customerName,
        string customerPhone,
        DateTime createdAt,
        string? description = null,
        string? additionalDetails = null)
    {
        Id = id;
        Type = type;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        CreatedAt = createdAt;
        Description = description;
        AdditionalDetails = additionalDetails;
    }

    // ------------------------------
    // Logic-layer update methods
    // ------------------------------

    /// <summary>
    /// Updates the geographic coordinates when the address is changed.
    /// </summary>
    public void UpdateCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}

/// <summary>
/// Enumeration representing order types according to company logic.
/// </summary>
internal enum OrderType
{
    Standard,
    Express,
    International,
    Other
}
