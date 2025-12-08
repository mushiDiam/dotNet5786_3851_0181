namespace BO;
using Helpers;
public class Courier
{
    public int Id { get; init; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsActive { get; set; }
    public double MaxDistancePreference { get; set; }
    public Transportaion Transport { get; set; }
    public DateTime JoinDate { get; init; }
    public int DeliveryCountOnTime { get; set; }
    public int DeliveryCountLate { get; set; }
    public OrderInProgress? ActiveOrder { get; set; }
    public override string ToString(){
        return Tools.ToStringProperty<Courier>(this);
    }


}
