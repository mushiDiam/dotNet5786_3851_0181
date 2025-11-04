using DalApi;
using DO;

namespace Dal;

public record Delivery
(
    int Id,                         //Delivery's ID
    int OrderId,                    //Order's ID
    int CourierId,                  //Courier's ID
    OrderType OrderType,            //What vehicle is being used to deliver
    DateTime StartOfDelivery,       //When had the delivery started
    double? ActualDistance,         //Distance to travel
    EndOfOrder? EndOfOrder,         //In what way did the Order end?
    DateTime? TimeOfDelivery        //The time the order was delivered 
)
{
}