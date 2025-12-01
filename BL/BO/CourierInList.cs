using Helpers;
namespace BO;
/// <param name="Id">Courier identifier.</param>
/// <param name="FullName">Full name of the courier (first + last).</param>
/// <param name="IsActive">Indicates if the courier is active.</param>
/// <param name="DeliveryType">Type of deliveries the courier performs.</param>
/// <param name="JoinDtae">Start time of work in the company.</param>
/// <param name="OrdersOnTime">Number of deliveries completed on time.</param>
/// <param name="OrdersLate">Number of deliveries completed late.</param>
/// <param name="CurrentOrderId">Identifier of the order currently in progress (nullable).</param>
public class CourierInList
{
    public int Id { get; init; }
    public string FullName { get; set; }
    public bool IsActive { get; set; }
    public DeliveryTypes DeliveryType { get; set; }
    public DateTime JoinDate { get; init; }
    public int OrdersOnTime { get; set; }
    public int OrdersLate { get; set; }
    public int? CurrentOrderId { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<CourierInList>(this);
    }
}
