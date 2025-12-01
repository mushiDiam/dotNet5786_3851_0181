namespace DalApi;
using DO;


public interface IConfig
{
    // IDs
    int NextDeliveryId { get; }
    int NextOrderId { get; }

    // Time and manager info
    DateTime Clock { get; set; }
    int ManagerId { get; set; }
    string ManagerPassword { get; set; }

    // Company details
    string? CompanyAddress { get; set; }
    double? CompanyLatitude { get; set; }
    double? CompanyLongitude { get; set; }
    double? MaxDeliveryDistance { get; set; }

    // Speed settings
    double AverageCarSpeed { get; set; }
    double AverageMotorcycleSpeed { get; set; }
    double AverageBikeSpeed { get; set; }
    double AverageWalkingSpeed { get; set; }

    // Time constraints
    TimeSpan MaxDeliveryTime { get; set; }
    TimeSpan RiskRange { get; set; }
    TimeSpan InactiveTime { get; set; }

    // Methods
    void Reset();
}
