namespace Dal;
/// <summary>
/// Course Entity
/// </summary>
/// <param name="StartDeliveryId">Id for the first delivery</param>
/// <param name="nextDeliveryId">Id for the next delivery</param>
/// <param name="NextDeliveryId">Id for the next delivery + get</param>
/// <param name="StartOrderId">Id for the first order</param>
/// <param name="nextOrderId">Id for the next order</param>
/// <param name="NextOrderId">Id for the next order +get</param>
/// <param name="Clock">Current time</param>
/// <param name="ManagerId">Id for the manager</param>
/// <param name="ManagerPassword">Password for the manager</param>
/// <param name="CompanyAdress">The company's adress</param>
/// <param name="CompanyLatitude">The company's latitude</param>
/// <param name="CompanyLongitude">The company's longitude</param>
/// <param name="MaxDeliveryDistance">Maximun delivery distance the company is capable of</param>
/// <param name="AverageCarSpeed">The average speed for the company's cars in km/h</param>
/// <param name="AverageMotorcycleSpeed">The average speed for the company's motorcycle in km/h</param>
/// <param name="AverageBikeSpeed">The average speed for the company's bike in km/h</param>
/// <param name="AverageWalkingSpeed">The average speed for the company's couriers in km/h</param>
/// <param name="MaxDeliveryTime">The maximum time for a delivery</param>
/// <param name="RiskRange">The time the order is consider in dangerous</param>
/// <param name="InactiveTime">The moment a courier is considered inactive</param>

internal static class Config
{
    internal const int StartDeliveryId = 1; 
    private static int nextDeliveryId = StartDeliveryId;
    internal static int NextDeliveryId { get => nextDeliveryId++; }

    internal const int StartOrderId = 1;
    private static int nextOrderId = StartOrderId;
    internal static int NextOrderId { get => nextOrderId++; } 
    internal static DateTime Clock { get; set; } = DateTime.Now;
    internal static int ManagerId { get; private set ; }
    internal static string ManagerPassword { get; private set; } = "";
    internal static string? CompanyAdress { get; private set; }
    internal static double? CompanyLatitude { get; private set; }
    internal static double? CompanyLongitude { get; private set; }
    internal static double? MaxDeliveryDistance { get; private set; }
    internal static double AverageCarSpeed { get; private set; }
    internal static double AverageMotorcycleSpeed { get; private set; }
    internal static double AverageBikeSpeed { get; private set; }
    internal static double AverageWalkingSpeed { get; private set; }
    internal static TimeSpan MaxDeliveryTime { get; private set; }
    internal static TimeSpan RiskRange { get; private set; }
    internal static TimeSpan InactiveTime { get; private set; }
    internal static void Reset(){
        nextDeliveryId = StartDeliveryId;
        nextOrderId = StartOrderId;
        Clock = DateTime.Now;
        ManagerPassword = "";
    }
}
