namespace DalApi;
using DO;


public interface IConfig
{
    // IDs
    int NextDeliveryId { get; }
    int NextOrderId { get; }

    // Time and manager info
    DateTime Clock { get; set; }
    int ManagerId { get; }
    string ManagerPassword { get; }

    // Company details
    string? CompanyAddress { get; }
    double? CompanyLatitude { get; }
    double? CompanyLongitude { get; }
    double? MaxDeliveryDistance { get; }

    // Speed settings
    double AverageCarSpeed { get; }
    double AverageMotorcycleSpeed { get; }
    double AverageBikeSpeed { get; }
    double AverageWalkingSpeed { get; }

    // Time constraints
    TimeSpan MaxDeliveryTime { get; }
    TimeSpan RiskRange { get; }
    TimeSpan InactiveTime { get; }

    // Methods
    void Reset();
}
