using DO;
using Helpers;
namespace BO;
/// <param name="Id">Sequential identifier of the order.</param>
/// <param name="OrderType">Type of the order.</param>
/// <param name="Description">Order description (nullable).</param>
/// <param name="FullAddress">Full order address.</param>
/// <param name="Latitude">Latitude of the order address (calculated from address).</param>
/// <param name="Longitude">Longitude of the order address (calculated from address).</param>
/// <param name="AirDistance">Air distance from the company.</param>
/// <param name="CustomerName">Full name of the customer.</param>
/// <param name="CustomerPhone">Customer phone number.</param>
/// <param name="Weight">Weight of order.</param>
/// <param name="Volume">Volume of order.</param>
/// <param name="Fragile">Is the order fragile.</param>
/// <param name="CreatedAt">Order creation time.</param>
/// <param name="ExpectedDeliveryTime">Expected delivery time (calculated in business layer).</param>
/// <param name="MaxDeliveryTime">Maximum delivery time (calculated in business layer).</param>
/// <param name="OrderStatus">Current order status (calculated in business layer).</param>
/// <param name="ScheduleStatus">Delivery on-time status (calculated in business layer).</param>
/// <param name="RemainingTime">Time remaining to complete delivery.</param>
/// <param name="Deliveries">List of deliveries for this order.</param>
public class Order
{
    public int Id { get; set; }
    public OrderTypes OrderType { get; set; }
    public string? Description { get; set; }
    public string FullAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AirDistance { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public int Weight { get; set; }
    public int Volume { get; set; }
    public bool Fragile { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpectedDeliveryTime { get; init; }
    public DateTime MaxDeliveryTime { get; set; }
    public OrderStatus OrderStatus{ get; set; }
    public ScheduleStatus ScheduleStatus { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public List<DeliveryPerOrderInList>? Deliveries { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<Order>(this);
    }
}
