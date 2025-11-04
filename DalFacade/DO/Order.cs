namespace DO;

public record Order
(    
    int Id,                                                     //Order's Id, for identification

    //Shipment
    string AdderssOfOrder,                                      //Where to ship the items
    double Latitude,                                            
    double Longttude,

    //Contact
    string Customername,
    string CustomerPhone,

    //Description of the order
    DateTime CreatedAt,
    bool Fragile,
    int weight,
    int volume,
    string? AdditionalDetails = null,
    string? Description = null
)
{
public Order() : this(0, "", 0, 0, "", "", DateTime.Now, false, 0, 0) { }
}