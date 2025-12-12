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

public enum ScheduleStatus{
    OnTime, Late, InRisk
}

public enum OrderStatus{
    Open, InProgress, Closed, Denied, Cancelled
}
public enum Transportation{
    Car, Motorcycle, Bike, Walking
}

public enum JobTypes{
    Courier , Manager
}

public enum OrderInListOptions{
    DeliveryId, OrderId, OrderType, AirDistance, OrderStatus, ScheduleStatus, RemainingTime, CompletionTime, DeliveryCount
}

public enum CourierInListOptions{
    Id, FullName, IsActive, DeliveryType, JoinDate, OrdersOnTime, OrdersLate, CurrentOrderId
}

public enum  Times{
    Minute, Hour, Day , Month , Year
}