namespace BO;

internal class Config
{
    public DateTime Clock {  get; set; }
    public TimeSpan DeliveryWindow { get; set; }
    public TimeSpan RiskRange { get; set; }
    public TimeSpan InactiveTime { get; set; }
    public string CompanyAddress { get; set; } = "";
    public double MaximumAirRange {  get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double MaxiumDistance { get; set; }
    public int ManagerId { get; set; }
    public string ManagerPassword { get; set; } = "";
    public double AverageCarSpeed { get; set; }
    public double AverageMotorbikeSpeed { get; set; }
    public double AverageBikeSpeed { get; set; }
    public double AverageWalkingSpeed { get; set; }
}
