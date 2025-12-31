using System;
using System.Linq.Expressions;
using BO;
using DalApi;
using DO;
namespace Helpers;

internal static class CourierManager
{
    private static IDal s_dal = Factory.Get;

    internal static ObserverManager Observers = new();

    internal static void CreateCourier(BO.Courier courier){
        try
        {
            DO.Courier DALCourier = ConvertToDal(courier);
            s_dal.Courier.Create(DALCourier);
        }
        catch (DalAlreadyExistsException ex)
        {
            throw new BlAlreadyExistsException("Courier with this ID already exists.", ex);
        }
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static BO.Courier Read(int id)
    {
        try
        {
            DO.Courier? dalCourier = s_dal.Courier.Read(id);
            if (dalCourier is null)
                throw new DalDoesNotExistException($"Courier with ID {id} does not exist.");
            return ConvertToBO(dalCourier);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
    }

   internal static CourierInList ConvertToCourierInList(BO.Courier courier)
    {
        return new CourierInList()
        {
            Id = courier.Id,
            FullName = courier.FullName,
            IsActive = courier.IsActive,
            DeliveryType = (DeliveryTypes)courier.Transport,
            JoinDate = courier.JoinDate,
            OrdersOnTime = courier.DeliveryCountOnTime,
            OrdersLate = courier.DeliveryCountLate,
            CurrentOrderId = courier.ActiveOrder?.OrderId
        };
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
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
        Observers.NotifyItemUpdated(courier.Id); //stage 5
        Observers.NotifyListUpdated(); //stage 5
    }
    public static void UpdateCourierActivity(DateTime oldClock, DateTime newClock)
    {
        DO.Courier[] couriers = s_dal.Courier.ReadAll().ToArray();
        foreach (var courier in couriers)
        {
            if (courier.Active && (newClock - oldClock).TotalDays > 30)
            {
                DO.Courier c = courier with { Active = false };
                s_dal.Courier.Update(c);
            }
        }
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static void Delete(int id)
    {
        var courierDeliveries = s_dal.Delivery.ReadAll(d => d.CourierId == id);
        //TODO: check if courier has active deliveries before deleting as well as if he ever handled deliveries
        try
        {
            s_dal.Courier.Delete(id);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
        }
        Observers.NotifyItemUpdated(id); //stage 5
        Observers.NotifyListUpdated(); //stage 5

    }

    internal static void DeleteAll()
    {
            s_dal.Courier.DeleteAll();
        Observers.NotifyListUpdated(); //stage 5
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
    internal static BO.Courier ConvertToBO(DO.Courier dalCourier)
    {
        if (dalCourier == null)
            throw new BlDoesNotExistException("DAL courier is null.");

        // basic mapping
        var bo = new BO.Courier
        {
            Id = dalCourier.Id,
            FullName = dalCourier.Name,
            PhoneNumber = dalCourier.Phone,
            IsActive = dalCourier.Active,
            Email = dalCourier.Email,
            MaxDistancePreference = dalCourier.MaxDeliveryDistance ?? 0.0,
            JoinDate = dalCourier.JoinDate,
            Transport = (BO.Transportation)dalCourier.OrderType // keep mapping consistent with DAL
        };

        try
        {
            // --- 1. read deliveries for this courier (snapshot) ---
            var deliveries = s_dal.Delivery.ReadAll(d => d.CourierId == dalCourier.Id)
                                         .ToList(); // materialize for safe reuse

            // --- 2. compute counts: on-time vs late (only for completed deliveries) ---
            int onTime = 0, late = 0;

            // fetch unique order ids referenced by completed deliveries to minimize DAL calls
            var completedDeliveries = deliveries
                .Where(d => d.EndOfOrder == DO.EndOfOrder.Completed && d.TimeOfDelivery.HasValue)
                .ToList();

            // If there are none, counts remain zero
            if (completedDeliveries.Count > 0)
            {
                // get distinct order ids involved
                var orderIds = completedDeliveries.Select(d => d.OrderId).Distinct().ToList();

                // read all related orders once
                var ordersMap = orderIds
                    .Select(id => new { id, order = s_dal.Order.Read(id) })
                    .ToDictionary(x => x.id, x => x.order);

                // config values for schedule calculation
                var maxDeliverySpan = s_dal.Config.MaxDeliveryTime;
                var riskRange = s_dal.Config.RiskRange;

                foreach (var del in completedDeliveries)
                {
                    if (!ordersMap.TryGetValue(del.OrderId, out var doOrder) || doOrder == null)
                        continue; // missing order — ignore or log

                    // define allowed max time for this order:
                    DateTime maxAllowed = doOrder.CreatedAt.Add(maxDeliverySpan);

                    // on-time if TimeOfDelivery <= maxAllowed
                    if (del.TimeOfDelivery.Value <= maxAllowed)
                        onTime++;
                    else
                        late++;
                }
            }

            // --- 3. find active delivery (the one started but not finished) ---
            // Rule: active delivery = delivery with EndOfOrder == null (or not Completed/Cancelled) and latest StartOfDelivery
            var activeDelivery = deliveries
                .Where(d => d.EndOfOrder == null)          // treated as in-progress
                .OrderByDescending(d => d.StartOfDelivery)
                .FirstOrDefault();

            BO.OrderInProgress? activeOrder = null;
            if (activeDelivery is not null)
            {
                // Prefer using OrderManager.Read to get BO.Order (it encapsulates conversions & schedule logic)
                BO.Order? boOrder = null;
                try
                {
                    boOrder = OrderManager.Read(activeDelivery.OrderId);
                }
                catch (DO.DalDoesNotExistException) { /* swallow and leave boOrder null */ }

                // Build minimal OrderInProgress based on available data.
                // Use values from boOrder when available (safer and consistent).
                activeOrder = new BO.OrderInProgress
                {
                    DeliveryId = activeDelivery.Id,
                    OrderId = activeDelivery.OrderId,
                    OrderType = boOrder?.OrderType ?? (BO.OrderTypes)activeDelivery.OrderType,
                    Description = boOrder?.Description ?? string.Empty,
                    Address = boOrder?.FullAddress ?? (boOrder == null ? string.Empty : boOrder.FullAddress),
                    AirDistance = boOrder?.AirDistance ?? 0,
                    ActualDistance = activeDelivery.ActualDistance,
                    CustomerName = boOrder?.CustomerName ?? string.Empty,
                    CustomerPhone = boOrder?.CustomerPhone ?? string.Empty,
                    CreatedAt = boOrder?.CreatedAt ?? DateTime.MinValue,
                    StartDeliveryTime = activeDelivery.StartOfDelivery,
                    ExpectedDeliveryTime = (DateTime)boOrder?.ExpectedDeliveryTime,
                    MaxiumDeliveryTime = boOrder?.MaxDeliveryTime ?? DateTime.MinValue,
                    OrderStatus = BO.OrderStatus.InProgress,
                    ScheduleStatus = boOrder?.ScheduleStatus ?? BO.ScheduleStatus.InRisk, // fallback
                    TimeLeftForDelivery = boOrder?.RemainingTime ?? TimeSpan.Zero
                };
            }

            // populate computed fields
            bo.DeliveryCountOnTime = onTime;
            bo.DeliveryCountLate = late;
            bo.ActiveOrder = activeOrder;

            return bo;
        }
        catch (DO.DalDoesNotExistException ex)
        {
            // translate DAL exceptions to BL
            throw new BlDoesNotExistException($"Related data for courier {dalCourier.Id} not found", ex);
        }
        catch (Exception ex)
        {
            throw new BlFailedToConvert("Failed converting courier from DAL to BO", ex);
        }
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
