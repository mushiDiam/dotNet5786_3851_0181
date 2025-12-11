using DO;
using Helpers;
namespace BO;

/// <param name="DeliveryId">Delivery identifier (nullable, not shown in UI).</param>
/// <param name="OrderId">Sequential identifier of the order.</param>
/// <param name="OrderType">Type of order.</param>
/// <param name="AirDistance">Air distance from the company.</param>
/// <param name="OrderStatus">Order status (calculated).</param>
/// <param name="ScheduleStatus">Delivery on-time status (calculated).</param>
/// <param name="RemainingTime">Time remaining to complete delivery.</param>
/// <param name="CompletionTime">Time to complete order (from first to last delivery).</param>
/// <param name="DeliveryCount">Number of deliveries executed for the order.</param>
public class OrderInList
{
    public int? DeliveryId { get; set; }
    public int OrderId { get; init; }
    public OrderTypes OrderType { get; set; }
    public double AirDistance { get; set; }
    public EndTypes EndType { get; set; }
    public ScheduleStatuses ScheduleStatus { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public int DeliveryCount { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<OrderInList>(this);
    }
}