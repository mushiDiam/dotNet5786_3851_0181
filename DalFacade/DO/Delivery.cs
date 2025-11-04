namespace DO;
using DalApi;

/// <summary>
/// Student Entity represents a student with all its props
/// </summary>
/// <param name="Id">Personal unique ID of the delivery </param>
/// <param name="OrderId">Personal unique ID of the order </param>
/// <param name="CourierId">Personal unique ID of the courier </param>
/// <param name="OrderType">What vehicle is being used to deliver </param>
/// <param name="StartOfDelivery">When had the delivery started </param>
/// <param name="ActualDistance">Distance to travel </param>
/// /// <param name="EndOfOrder">In what way did the Order end? </param>
/// <param name="TimeOfDelivery">The time the order was delivered </param>
/// </summary>
public record Delivery
(
    int Id,                         
    int OrderId,                    
    int CourierId,                  
    OrderType OrderType,            
    DateTime StartOfDelivery,       
    double? ActualDistance,         
    EndOfOrder? EndOfOrder,         
    DateTime? TimeOfDelivery        
)
{
    public Delivery() : this(0, 0, 0, default, default, null, null, null) { }
}