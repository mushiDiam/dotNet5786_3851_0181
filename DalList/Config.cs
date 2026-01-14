using System.Runtime.CompilerServices;

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
/// <param name="CompanyAddress">The company's address</param>
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
    internal static int NextDeliveryId { [MethodImpl(MethodImplOptions.Synchronized)] get => nextDeliveryId++; }

    internal const int StartOrderId = 1;
    private static int nextOrderId = StartOrderId;
    internal static int NextOrderId { [MethodImpl(MethodImplOptions.Synchronized)] get => nextOrderId++; } 
    internal static DateTime Clock { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; } = DateTime.Now;
    internal static int ManagerId { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static string ManagerPassword { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; } = "";
    internal static string? CompanyAddress { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double? CompanyLatitude { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double? CompanyLongitude { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double? MaxDeliveryDistance { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double AverageCarSpeed { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double AverageMotorcycleSpeed { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double AverageBikeSpeed { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double AverageWalkingSpeed { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static TimeSpan MaxDeliveryTime { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static TimeSpan RiskRange { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static TimeSpan InactiveTime { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Reset(){
        nextDeliveryId = StartDeliveryId;
        nextOrderId = StartOrderId;
        Clock = DateTime.Now;
        ManagerPassword = "";
    }
}
