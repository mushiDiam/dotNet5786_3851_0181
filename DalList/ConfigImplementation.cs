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
    public DateTime Clock { get; set; } = DateTime.Now;
    public int ManagerId { get; private set; }
    public string ManagerPassword { get; private set; } = "";

    // Company details
    public string? CompanyAdress { get; private set; }
    public double? CompanyLatitude { get; private set; }
    public double? CompanyLongitude { get; private set; }
    public double? MaxDeliveryDistance { get; private set; }

    // Speed settings
    public double AverageCarSpeed { get; private set; }
    public double AverageMotorcycleSpeed { get; private set; }
    public double AverageBikeSpeed { get; private set; }
    public double AverageWalkingSpeed { get; private set; }

    // Time constraints
    public TimeSpan MaxDeliveryTime { get; private set; }
    public TimeSpan RiskRange { get; private set; }
    public TimeSpan InactiveTime { get; private set; }

    // Reset method
    public void Reset(){
        nextDeliveryId = StartDeliveryId;
        nextOrderId = StartOrderId;
        Clock = DateTime.Now;
        ManagerPassword = "";
    }
}
