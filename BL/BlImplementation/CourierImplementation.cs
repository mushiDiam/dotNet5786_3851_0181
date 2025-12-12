namespace BlImplementation;
using System.Collections.Generic;
using System.Linq;
using BlApi;
using BLImplementation;
using BO;
using DalApi;
using DO;
using global::Helpers;

internal class CourierImplementation : BlApi.ICourier
{
    public void Add(int id, BO.Courier courierId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can add couriers");
        CourierManager.CreateCourier(courierId);
    }

    public void Delete(int id, int courierId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can delete couriers");
        CourierManager.Delete(courierId);
    }

    public BO.Courier Details(int id, int courierId)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can access couriers");
        return CourierManager.Read(courierId);
    }

    public JobTypes EnterProgram(int id)
    {
        var match = CourierManager.ReadAll().FirstOrDefault(c => c.Id == id);
        if (match is null)
            throw new BlDoesNotExistException($"No courier with id {id} exists");
        if (AdminManager.IsAdmin(id))
            return JobTypes.Manager;
        return JobTypes.Courier;
    }

    public IEnumerable<CourierInList> GetCouriers(int id, bool? includeInactive, CourierInListOptions? sort)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can access couriers");

        IEnumerable<BO.Courier> couriers = CourierManager.ConvertToBOList(CourierManager.ReadAll());
       
        if (includeInactive.HasValue) 
            couriers = couriers.Where(c => c.IsActive == includeInactive.Value);
        
        if (sort is null)
            couriers = couriers.OrderBy(c => c.Id);
        else
            switch(sort.Value)
            {
                case CourierInListOptions.Id:
                    couriers = couriers.OrderBy(c => c.Id);
                    break;
                case CourierInListOptions.FullName:
                    couriers = couriers.OrderBy(c => c.FullName);
                    break;
                case CourierInListOptions.IsActive:
                        couriers = couriers.OrderByDescending(c => c.IsActive);
                    break;
                case CourierInListOptions.DeliveryType:
                        couriers = couriers.OrderBy(c => c.Transport);
                    break;
                case CourierInListOptions.JoinDate:
                    couriers = couriers.OrderBy(c => c.JoinDate);
                    break;
                case CourierInListOptions.OrdersOnTime:
                    couriers = couriers.OrderByDescending(c => c.DeliveryCountOnTime);
                    break;
                case CourierInListOptions.OrdersLate:
                    couriers = couriers.OrderByDescending(c => c.DeliveryCountLate);
                    break;
                case CourierInListOptions.CurrentOrderId:
                    couriers = couriers.OrderBy(c => c.ActiveOrder);
                    break;
                default:
                    break;
            }

        return couriers.Select(CourierManager.ConvertToCourierInList);


    }

    public void UpdateDetails(int id, BO.Courier courier)
    {
        if (!AdminManager.IsAdmin(id))
            throw new BlUnauthorizedAccessException("Only an admin can access couriers");
        CourierManager.Update(courier);
    }
}
