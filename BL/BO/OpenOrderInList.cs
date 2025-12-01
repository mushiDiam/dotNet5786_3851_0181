using DO;
using Helpers;
namespace BO;

/// <param name="CourierId">Courier identifier (nullable, not shown in UI).</param>
/// <param name="OrderId">Sequential identifier of the order.</param>
/// <param name="OrderType">Type of order.</param>
/// <param name="Weight">Weight of order.</param>
/// <param name="Volume">Volume of order.</param>
/// <param name="Fragile">Is the order fragile.</param>
/// <param name="FullAddress">Full address of the order.</param>
/// <param name="AirDistance">Air distance from the company (calculated).</param>
/// <param name="ActualDistance">Actual distance to the order (nullable).</param>
/// <param name="EstimatedTime">Estimated time to delivery (nullable).</param>
/// <param name="ScheduleStatus">Delivery on-time status (calculated).</param>
/// <param name="RemainingTime">Time remaining to complete delivery.</param>
/// <param name="MaxDeliveryTime">Maximum delivery time (calculated).</param>
public class OpenOrderInList
{
    public int? CourierId { get; init; }
    public int OrderId { get; init; }
    public OrderTypes OrderType { get; set; }
    public int weight { get; set; }
    public int volume { get; set; }
    public bool fragile { get; set; }
    public string FullAddress { get; set; }
    public double AirDistance { get; set; }
    public double? ActualDistance { get; set; }
    public TimeSpan? EstimatedTime { get; init; }
    public ScheduleStatuses ScheduleStatus { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public DateTime MaxDeliveryTime { get; init; }
    public override string ToString(){
        return Tools.ToStringProperty<OpenOrderInList>(this);
    }
}