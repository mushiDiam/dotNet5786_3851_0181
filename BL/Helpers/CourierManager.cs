using BO;
using DalApi;
using DO;
using System.Linq.Expressions;
namespace Helpers;

internal static class CourierManager{
    private static IDal s_dal = Factory.Get;

    internal static void Create(BO.Courier courier){
        try
        {
            DO.Courier DALCourier = ConvertToDal(courier);
            s_dal.Courier.Create(DALCourier);
        }
        catch (BlAlreadyExistsException ex)
        {
            throw new BlAlreadyExistsException("Courier with this ID already exists.", ex);
        }
    }
    internal static BO.Courier Read(int id)
    {
        try
        {
            DO.Courier dalCourier = s_dal.Courier.Read(id);
            return ConvertToBO(dalCourier);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
    }

   

    internal static IEnumerable<DO.Courier> ReadAll()
    {
            return s_dal.Courier.ReadAll();
    }
    internal static void Update(BO.Courier courier)
    {
        try
        {
           s_dal.Courier.Update(ConvertToDal(courier));
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
    }

    internal static void Delete(int id)
    {
        try
        {
            //TODO: check if courier has active deliveries before deleting as well as if he ever handled deliveries
            s_dal.Courier.Delete(id);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
    }
    
    internal static void DeleteAll()
    {
            s_dal.Courier.DeleteAll();
    }
    public static DO.Courier ConvertToDal(BO.Courier courier){
        return new DO.Courier(){
            Id = courier.Id,
            Name = courier.FullName,
            Phone = courier.PhoneNumber,
            Active = courier.IsActive,
            Email= courier.Email,
            MaxDeliveryDistance = courier.MaxDistancePreference,
            JoinDate = courier.JoinDate,
            OrderType = (DO.OrderType)courier.Transport
        };
    }
    internal static BO.Courier ConvertToBO(DO.Courier? dalCourier)
    {
        return new BO.Courier(){
            Id = dalCourier.Id,
            FullName = dalCourier.Name,
            PhoneNumber = dalCourier.Phone,
            IsActive = dalCourier.Active,
            Email = dalCourier.Email,
            MaxDistancePreference = (double)dalCourier.MaxDeliveryDistance,
            JoinDate = dalCourier.JoinDate,
            Transport = (BO.Transportation)dalCourier.OrderType
        };
    }
    internal static IEnumerable<BO.Courier> ConvertToBOList(IEnumerable<DO.Courier> dalCouriers)
    {
        return dalCouriers.Select(dalCourier => ConvertToBO(dalCourier));
    }
    internal static bool Exists(int id)
    {
        return ReadAll().Any(c => c.Id == id);
    }
}
