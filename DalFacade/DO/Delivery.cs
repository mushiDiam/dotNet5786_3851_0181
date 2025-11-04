using DalApi;
using DO;

namespace Dal;

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
}