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
    public int ManagerId { get; set; }
    public string ManagerPassword { get; set; } = "";

    // Company details
    public string? CompanyAddress { get; set; }
    public double? CompanyLatitude { get; set; }
    public double? CompanyLongitude { get; set; }
    public double? MaxDeliveryDistance { get; set; }

    // Speed settings
    public double AverageCarSpeed { get; set; }
    public double AverageMotorcycleSpeed { get; set; }
    public double AverageBikeSpeed { get; set; }
    public double AverageWalkingSpeed { get; set; }

    // Time constraints
    public TimeSpan MaxDeliveryTime { get; set; }
    public TimeSpan RiskRange { get; set; }
    public TimeSpan InactiveTime { get; set; }

    // Reset method
    public void Reset(){
        nextDeliveryId = StartDeliveryId;
        nextOrderId = StartOrderId;
        Clock = DateTime.Now;
        ManagerPassword = "";
    }
}
