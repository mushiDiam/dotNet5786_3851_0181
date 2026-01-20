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
    // --- Validation Helpers ---
    private static bool IsValidId(int id)
    {
        // ID must be between 100000000 and 999999999 (exactly 9 digits)
        return id >= 100000000 && id <= 999999999;
    }

    internal const int StartDeliveryId = 1; 
    private static int nextDeliveryId = StartDeliveryId;
    internal static int NextDeliveryId { [MethodImpl(MethodImplOptions.Synchronized)] get => nextDeliveryId++; }

    internal const int StartOrderId = 1;
    private static int nextOrderId = StartOrderId;
    internal static int NextOrderId { [MethodImpl(MethodImplOptions.Synchronized)] get => nextOrderId++; } 
    internal static DateTime Clock { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; } = DateTime.Now;
    
    // ManagerId with validation
    private static int _managerId;
    internal static int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _managerId;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (!IsValidId(value))
                throw new DO.DalInvalidValueException("Manager ID must be exactly 9 digits");
            _managerId = value;
        }
    }

    // ManagerPassword with validation
    private static string _managerPassword = "";
    internal static string ManagerPassword
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _managerPassword;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DO.DalInvalidValueException("Manager password cannot be empty");
            _managerPassword = value;
        }
    }

    internal static string? CompanyAddress { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double? CompanyLatitude { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
    internal static double? CompanyLongitude { [MethodImpl(MethodImplOptions.Synchronized)] get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

    // MaxDeliveryDistance with validation
    private static double? _maxDeliveryDistance;
    internal static double? MaxDeliveryDistance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _maxDeliveryDistance;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value.HasValue && value.Value < 0)
                throw new DO.DalInvalidValueException("Max delivery distance cannot be negative");
            _maxDeliveryDistance = value;
        }
    }

    // AverageCarSpeed with validation
    private static double _averageCarSpeed;
    internal static double AverageCarSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _averageCarSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average car speed cannot be negative");
            _averageCarSpeed = value;
        }
    }

    // AverageMotorcycleSpeed with validation
    private static double _averageMotorcycleSpeed;
    internal static double AverageMotorcycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _averageMotorcycleSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average motorcycle speed cannot be negative");
            _averageMotorcycleSpeed = value;
        }
    }

    // AverageBikeSpeed with validation
    private static double _averageBikeSpeed;
    internal static double AverageBikeSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _averageBikeSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average bike speed cannot be negative");
            _averageBikeSpeed = value;
        }
    }

    // AverageWalkingSpeed with validation
    private static double _averageWalkingSpeed;
    internal static double AverageWalkingSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _averageWalkingSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average walking speed cannot be negative");
            _averageWalkingSpeed = value;
        }
    }

    // MaxDeliveryTime with validation
    private static TimeSpan _maxDeliveryTime;
    internal static TimeSpan MaxDeliveryTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _maxDeliveryTime;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Max delivery time cannot be negative");
            _maxDeliveryTime = value;
        }
    }

    // RiskRange with validation
    private static TimeSpan _riskRange;
    internal static TimeSpan RiskRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _riskRange;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Risk range cannot be negative");
            _riskRange = value;
        }
    }

    // InactiveTime with validation
    private static TimeSpan _inactiveTime;
    internal static TimeSpan InactiveTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _inactiveTime;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Inactive time cannot be negative");
            _inactiveTime = value;
        }
    }
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Reset(){
        nextDeliveryId = StartDeliveryId;
        nextOrderId = StartOrderId;
        Clock = DateTime.Now;
        _managerPassword = "10"; // Reset to default password
        _managerId = 0; // Reset manager ID (direct assignment to bypass validation)
    }
}
