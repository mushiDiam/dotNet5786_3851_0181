namespace BO;
public enum DeliveryTypes{
    Regular, Express, Fragile
}

public enum OrderTypes  {
    Food, Gifts, Health, Supplies, Pets
}

public enum OrderStatuses{
    InProgress, Completed, Cancelled
}

public enum ScheduleStatuses{
    OnTime, Late, Early
}

public enum EndTypes{
    Completed, Denied, Canceled, Unreached, Failed
}
public enum Transportaion{
    Car, Motorcycle, Bike, Walking
}