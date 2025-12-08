namespace BO;
public enum DeliveryTypes{
    Regular, Express, Fragile
}

public enum OrderTypes  {
    Food, Gifts, Health, Supplies, Pets
}

public enum EndTypes{
    InProgress, Completed, Cancelled
}

public enum ScheduleStatuses{
    OnTime, Late, AtRisk
}

public enum OrderStatuses{
    Completed, Denied, Canceled, Unreached, Failed
}
public enum Transportaion{
    Car, Motorcycle, Bike, Walking
}

public enum JobTypes{
    Courier , Manager
}

public enum OrderOptions{
    Id, Type, Latitude, Longitude , AirDistance , Weight, Volume, Fragile, CreatedAt, ExpectedDeliveryTime, MaxDeliveryTime, OrderStatus, ScheduleStatus, RemainingTime
}

public enum CourierOptions{
    Id ,FullName , PhoneNumber ,Email ,Password ,IsActive , MaxDistancePreference, Transport , JoinDate, DeliveryCountOnTime, DeliveryCountLate,
}

public enum  Times{
    Minute, Hour, Day , Month , Year
}