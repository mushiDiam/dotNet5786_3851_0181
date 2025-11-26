using DO;

namespace BO;

/// <param name="DeliveryId">Sequential identifier of the delivery (not shown in UI).</param>
/// <param name="OrderId">Sequential identifier of the order.</param>
/// <param name="OrderType">Type of the order.</param>
/// <param name="FullAddress">Full address of the order.</param>
/// <param name="DeliveryType">Type of delivery.</param>
/// <param name="ActualDistance">Actual delivery distance (nullable).</param>
/// <param name="CompletionTime">Time to complete delivery.</param>
/// <param name="EndType">Delivery completion type (nullable).</param>
public class ClosedDeliveryInList
{
    public int DeliveryId { get; init; }
    public int OrderId { get; init; }
    public OrderTypes OrderType { get; set; }
    public string FullAddress { get; set; }
    public DeliveryTypes DeliveryType { get; set; }
    public double? ActualDistance { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public EndTypes? EndType { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<ClosedDeliveryInList>(this);
    }
}