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
public enum Transportation{
    Car, Motorcycle, Bike, Walking
}

public enum JobTypes{
    Courier , Manager
}

public enum OrderOptions{
    Id, Type, Latitude, Longitude , AirDistance , Weight, Volume, Fragile, CreatedAt, ExpectedDeliveryTime, MaxDeliveryTime, OrderStatus, ScheduleStatus, RemainingTime
}

public enum CourierInListOptions{
    Id, FullName, IsActive, DeliveryType, JoinDate, OrdersOnTime, OrdersLate, CurrentOrderId
}

public enum  Times{
    Minute, Hour, Day , Month , Year

}