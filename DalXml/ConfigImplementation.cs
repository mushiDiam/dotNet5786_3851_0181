namespace Dal;
using DalApi;


internal class ConfigImplementation : IConfig
{
    // IDs
    private const int StartDeliveryId = 1;
    private int nextDeliveryId = StartDeliveryId;
    public int NextDeliveryId => nextDeliveryId++;

    private const int StartOrderId = 1;
    private int nextOrderId = StartOrderId;
    public int NextOrderId => nextOrderId++;

    // Time and manager info 
    public DateTime Clock{
        get => Config.Clock;
        set => Config.Clock = value;
    }
    public int ManagerId{
        get => Config.ManagerId;
        set => Config.ManagerId = value;
    }
    public string ManagerPassword{
        get => Config.ManagerPassword;
        set => Config.ManagerPassword = value;
    }

    // Company details
    public string? CompanyAddress{
        get => Config.CompanyAddress;
        set => Config.CompanyAddress = value;
    }
    public double? CompanyLatitude{
        get => Config.CompanyLatitude;
        set => Config.CompanyLatitude = value;
    }
    public double? CompanyLongitude { 
        get => Config.CompanyLongitude;
        set => Config.CompanyLongitude = value;
    }
    public double? MaxDeliveryDistance { 
        get => Config.MaxDeliveryDistance;
        set => Config.MaxDeliveryDistance = value;
    }

    // Speed settings
    public double AverageCarSpeed { 
        get => Config.AverageCarSpeed;
        set => Config.AverageCarSpeed = value;
    }
    public double AverageMotorcycleSpeed { 
        get => Config.AverageMotorcycleSpeed;
        set => Config.AverageMotorcycleSpeed = value;
    }
    public double AverageBikeSpeed { 
        get => Config.AverageBikeSpeed;
        set => Config.AverageBikeSpeed = value;
    }
    public double AverageWalkingSpeed { 
        get => Config.AverageWalkingSpeed;
        set => Config.AverageWalkingSpeed = value;
    }

    // Time constraints
    public TimeSpan MaxDeliveryTime {
        get => Config.MaxDeliveryTime;
        set => Config.MaxDeliveryTime = value;
    }
    public TimeSpan RiskRange { 
        get => Config.RiskRange;
        set => Config.RiskRange = value;
    }
    public TimeSpan InactiveTime { 
        get => Config.InactiveTime;
        set => Config.InactiveTime = value;
    }

    // Reset method
    public void Reset(){
        Config.Reset();
    }
}
