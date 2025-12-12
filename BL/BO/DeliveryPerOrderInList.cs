using System.ComponentModel;
using Helpers;
namespace BO;

/// <param name="DeliveryId">Sequential identifier of the delivery (not shown in UI).</param>
/// <param name="CourierId">Identifier of the courier (nullable, not shown in UI).</param>
/// <param name="CourierName">Full name of the courier.</param>
/// <param name="DeliveryType">Type of delivery.</param>
/// <param name="StartTime">Start time of the delivery.</param>
/// <param name="
/// 
/// ">Delivery completion type (nullable).</param>
/// <param name="EndTime">Delivery completion time (nullable).</param>
public class DeliveryPerOrderInList
{
    public int DeliveryId { get; init; }
    public int? CourierId { get; set; }
    public string CourierName { get; set; }
    public DeliveryTypes DeliveryType { get; set; }
    public DateTime StartTime { get; init; }
    public OrderStatus? OrderStatus { get; set; }
    public DateTime? EndTime { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<DeliveryPerOrderInList>(this);
    }
}
