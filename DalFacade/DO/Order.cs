namespace DO;
///  <summary>
///  Student Entity represents a student with all its props
///  </summary>
///  <param name="Id">Personal unique ID of the order </param>
///  <param name="AdderssOfOrder"> Where to ship the items </param>
///  <param name="Latitude">Coordinates </param>
///  <param name="Longtitude">Coordinates </param>
///  <param name="CustomerName">The name of the customer</param>
///  <param name="CustomerPhone">Phone number of the customer </param>
///  <param name="CreatedAt">When was the order created </param>
///  <param name="Fragile">Is it fragile? </param>
///  <param name="Weight">What is it's weight? </param>
///  <param name="Volume">What is it's volume? </param>
///  <param name="AdditionalDetails">For more information about the Order </param>
///  <param name="Description">The description of the order </param>
///  </summary>
public record Order
(    
    int Id,                                                     

    //Shipment
    string AdderssOfOrder,                                    
    double Latitude,                                            
    double Longtitude,

    //Contact
    string CustomerName,
    string CustomerPhone,

    //Description of the order
    DateTime CreatedAt,
    bool Fragile,
    int Weight,
    int Volume,
    OrderType OrderType,
    string? AdditionalDetails = null,
    string? Description = null
)
{
public Order() : this(0, "", 0, 0, "", "", DateTime.Now, false,0, 0, 0) { }
}