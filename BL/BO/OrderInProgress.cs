using Helpers;
namespace BO;
/// <param name="DeliveryId">Unique identifier of the delivery (not shown in UI).</param>
/// <param name="OrderId">Sequential identifier of the order.</param>
/// <param name="OrderType">The type of the order.</param>
/// <param name="Description">Order description (nullable).</param>
/// <param name="Address">Full address of the order.</param>
/// <param name="AirDistance">Air distance from the company (calculated in business layer).</param>
/// <param name="ActualDistance">Actual delivery distance (nullable, calculated in business layer).</param>
/// <param name="CustomerName">Full name of the customer.</param>
/// <param name="CustomerPhone">Customer phone number.</param>
/// <param name="CreatedAt">Order creation time.</param>
/// <param name="StartDeliveryTime">Delivery start time.</param>
/// <param name="ExpectedDeliveryTime">Expected delivery time (calculated in business layer).</param>
/// <param name="MaxiumDeliveryTime">Maximum delivery time (calculated in business layer).</param>
/// <param name="OrderStatus">Current order status (calculated in business layer).</param>
/// <param name="ScheduleStatus">Delivery on-time status (calculated in business layer).</param>
/// <param name="TimeLeftForDelivery">Time remaining to complete delivery (calculated in business layer).</param>

public class OrderInProgress
{
    public int DeliveryId { get; init; }
    public int OrderId { get; init; }
    public OrderTypes OrderType { get; set; }
    public string? Description { get; set; }
    public string Address { get; set; }
    public double AirDistance { get; set; }
    public double? ActualDistance { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime StartDeliveryTime { get; set; }
    public DateTime ExpectedDeliveryTime { get; set; }
    public DateTime MaxiumDeliveryTime { get; set; }
    public OrderStatuses OrderStatus { get; set; }
    public ScheduleStatuses ScheduleStatus { get; set; }
    public TimeSpan TimeLeftForDelivery { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<OrderInProgress>(this);
    }
}
