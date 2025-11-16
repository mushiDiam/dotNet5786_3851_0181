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
    }
    public string ManagerPassword{
        get => Config.ManagerPassword;
    }

    // Company details
    public string? CompanyAdress{
        get => Config.CompanyAdress;
    }
    public double? CompanyLatitude{
        get => Config.CompanyLatitude;
    }
    public double? CompanyLongitude { 
        get => Config.CompanyLongitude;
    }
    public double? MaxDeliveryDistance { 
        get => Config.MaxDeliveryDistance;
    }

    // Speed settings
    public double AverageCarSpeed { 
        get => Config.AverageCarSpeed;
    }
    public double AverageMotorcycleSpeed { 
        get => Config.AverageMotorcycleSpeed;
    }
    public double AverageBikeSpeed { 
        get => Config.AverageBikeSpeed;
    }
    public double AverageWalkingSpeed { 
        get => Config.AverageWalkingSpeed;
    }

    // Time constraints
    public TimeSpan MaxDeliveryTime {
        get => Config.MaxDeliveryTime;
    }
    public TimeSpan RiskRange { 
        get => Config.RiskRange;
    }
    public TimeSpan InactiveTime { 
        get => Config.InactiveTime;
    }

    // Reset method
    public void Reset(){
        Config.Reset();
    }
}
