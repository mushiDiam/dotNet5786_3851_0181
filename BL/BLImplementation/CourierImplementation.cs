namespace BLImplementation;
using BlApi;
using BO;
using DO;
using Helpers;
using System;
using System.Collections.Generic;

internal class CourierImplementation : ICourier
{
    public void Add(int id, BO.Courier courier)
    {
        if(!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admins can add couriers.");
        CourierManager.Create(courier);
    }

    public void Delete(int id, int courierId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admins can delete couriers.");
        CourierManager.Delete(courierId);
    }

    public BO.Courier Details(int id, int courierId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admins can read a courier.");
        return CourierManager.Read(courierId);
    }

    public JobTypes EnterProgram(int id)
    {
        if (AdminManager.IsAdmin(id))
            return JobTypes.Manager;
        else if (CourierManager.Exists(id))
           return JobTypes.Courier;
        else    
            throw new BlUnauthorizedAccessException("ID does not exist in the system.");
    }

    public IEnumerable<CourierInList> GetCouriers(int id, bool? includeInactive, CourierInListOptions? sort)
    {
        if(!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only admins can read couriers.");
        IEnumerable<BO.Courier> couriers = CourierManager.ConvertToBOList(CourierManager.ReadAll());
        if (includeInactive.HasValue)
            couriers = couriers.Where(couriers => couriers.IsActive == includeInactive.Value);
        var lst = couriers.Select(courier => new CourierInList
        {
            Id = courier.Id,
            FullName = courier.FullName,
            IsActive = courier.IsActive,
            DeliveryType = (DeliveryTypes)courier.Transport,
            JoinDate = courier.JoinDate,
            OrdersOnTime = courier.DeliveryCountOnTime,
            OrdersLate = courier.DeliveryCountLate,
            CurrentOrderId = courier.ActiveOrder?.OrderId
        }).ToList();
        if (sort is null)
            return lst.OrderBy(c => c.Id).ToList();
        switch (sort)
        {
            case CourierInListOptions.Id:
                lst = lst.OrderBy(c => c.Id).ToList();
                break;
            case CourierInListOptions.FullName:
                lst = lst.OrderBy(c => c.FullName).ToList();
                break;
            case CourierInListOptions.IsActive:
                lst = lst.OrderByDescending(c => c.IsActive).ToList();
                break;
            case CourierInListOptions.DeliveryType:
                lst = lst.OrderBy(c => c.DeliveryType).ToList();
                break;
            case CourierInListOptions.JoinDate:
                lst = lst.OrderBy(c => c.JoinDate).ToList();
                break;
            case CourierInListOptions.OrdersOnTime:
                lst = lst.OrderByDescending(c => c.OrdersOnTime).ToList();
                break;
            case CourierInListOptions.OrdersLate:
                lst = lst.OrderByDescending(c => c.OrdersLate).ToList();
                break;
            case CourierInListOptions.CurrentOrderId:
                lst = lst.OrderBy(c => c.CurrentOrderId).ToList();
                break;
            default:
                break;
        }
        return lst;
    }

    public void UpdateDetails(int id, BO.Courier courier)
    {
        throw new NotImplementedException();
    }
}
