using System;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using BO;
using DalApi;
using DO;

namespace Helpers;

internal static class CourierManager
{
    private static IDal s_dal = Factory.Get;

    private static readonly AsyncMutex s_periodicMutex = new(); //stage 7
    private static readonly AsyncMutex s_simulationMutex = new(); //stage 7
    internal static ObserverManager Observers = new();
    private static readonly Random s_rand = new();
    internal static void CreateCourier(BO.Courier courier){
        lock (AdminManager.BlMutex) { 
            try
            {
               DO.Courier DALCourier = ConvertToDal(courier);
               s_dal.Courier.Create(DALCourier);
            }
            catch (DalAlreadyExistsException ex)
            {
                throw new BlAlreadyExistsException("Courier with this ID already exists.", ex);
            }
        }
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static BO.Courier Read(int id)
    {
        try
        {
            lock (AdminManager.BlMutex)
            {
                DO.Courier? dalCourier = s_dal.Courier.Read(id);
                if (dalCourier is null)
                    throw new DalDoesNotExistException($"Courier with ID {id} does not exist.");
                return ConvertToBO(dalCourier);
            }
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
            Transport = (Transportation)courier.Transport,
            JoinDate = courier.JoinDate,
            OrdersOnTime = courier.DeliveryCountOnTime,
            OrdersLate = courier.DeliveryCountLate,
            CurrentOrderId = courier.ActiveOrder?.OrderId
        };
    }

    internal static IEnumerable<DO.Courier> ReadAll()
    {
        lock (AdminManager.BlMutex)
            return s_dal.Courier.ReadAll().ToList(); // Forces immediate execution
    }
    internal static void Update(BO.Courier courier)
    {
        if (courier is null)
            throw new BlInvalidValueException("Courier cannot be null.");

        // Ensure courier exists and get current state
        BO.Courier existing;
        try
        {
            existing = Read(courier.Id);
        }
        catch (BlDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Courier with ID {courier.Id} does not exist.", ex);
        }

        // If courier has an active order, do not allow changing vehicle (Transport) or active flag
        if (existing.ActiveOrder != null)
        {
            if (courier.Transport != existing.Transport)
                throw new BlInvalidOperationException("Cannot change courier vehicle while they have an active order.");

            if (courier.IsActive != existing.IsActive)
                throw new BlInvalidOperationException("Cannot change courier active status while they have an active order.");
        }

        // Validate company max delivery distance configured
        lock (AdminManager.BlMutex)
        {
            double? companyMax = s_dal.Config.MaxDeliveryDistance;
            if (!companyMax.HasValue)
                throw new BlInvalidOperationException("Company maximum delivery distance is not configured.");

            // Validate courier max distance does not exceed company maximum
            if (courier.MaxDistancePreference > companyMax.Value)
                throw new BlInvalidValueException($"Courier maximum distance ({courier.MaxDistancePreference} km) cannot exceed company maximum ({companyMax.Value} km).");

            // Validate company coordinates configured
            if (!s_dal.Config.CompanyLatitude.HasValue || !s_dal.Config.CompanyLongitude.HasValue)
                throw new BlInvalidValueException("Company coordinates are not configured.");

            try
            {
                s_dal.Courier.Update(ConvertToDal(courier));
            }
            catch (DalDoesNotExistException ex)
            {
                throw new BlDoesNotExistException("Courier with this ID Doesn't exists.", ex);
            }
        }
        Observers.NotifyItemUpdated(courier.Id); //stage 5
        Observers.NotifyListUpdated(); //stage 5
    }
    public static void UpdateCourierActivity(DateTime oldClock, DateTime newClock)
    {
        // Check and prevent double entry into the method (re-entry protection)
        if (s_periodicMutex.CheckAndSetInProgress())
            return;

        List<int> couriersChanged = new();

        try
        {
            lock (AdminManager.BlMutex)
            {
                // 1. Retrieve inactivity time threshold from configuration
                // The value is of type TimeSpan (e.g., 30 days)
                TimeSpan inactiveThreshold = s_dal.Config.InactiveTime;

                // 2. Fetch all currently active couriers
                List<DO.Courier> activeCouriers = s_dal.Courier.ReadAll(c => c.Active).ToList();

                foreach (var courier in activeCouriers)
                {
                    // Check A: Is the courier currently in the middle of a delivery? 
                    // If so, do not touch them (we can't deactivate a working courier)
                    bool isCurrentlyWorking = s_dal.Delivery.ReadAll(d =>
                        d.CourierId == courier.Id && d.EndOfOrder == null).Any();

                    if (isCurrentlyWorking) continue;

                    // Check B: Find the time of the last delivery performed by the courier
                    var lastDeliveryDate = s_dal.Delivery.ReadAll(d => d.CourierId == courier.Id && d.EndOfOrder != null && d.TimeOfDelivery.HasValue)
                                     .Max(d => d.TimeOfDelivery);

                    // If the courier has never worked, we skip them (or you can decide to deactivate them)
                    if (lastDeliveryDate == null) continue;

                    // Check C: Has enough time passed since the last delivery?
                    // Comparison: (Time passed since last delivery) > (Threshold defined in configuration)
                    if ((newClock - lastDeliveryDate.Value) > inactiveThreshold)
                    {
                        // Deactivate the courier
                        DO.Courier updated = courier with { Active = false };
                        s_dal.Courier.Update(updated);

                        couriersChanged.Add(courier.Id);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore specific errors to avoid stopping the loop for all couriers
        }
        finally
        {
            // Release the mutex
            s_periodicMutex.UnsetInProgress();
        }

        // Update the display (observers) if there were any changes
        if (couriersChanged.Any())
            Observers.NotifyListUpdated();
    }
    internal static void Delete(int id)
    {
        lock (AdminManager.BlMutex)
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
        }
        Observers.NotifyItemUpdated(id); //stage 5
        Observers.NotifyListUpdated(); //stage 5

    }

    internal static void DeleteAll()
    {
        lock (AdminManager.BlMutex)
            s_dal.Courier.DeleteAll();
        Observers.NotifyListUpdated(); //stage 5
    }

    // -------------------------------------------------------
    // ConvertToDal: INCLUDE the Password field (was missing)
    // -------------------------------------------------------
    public static DO.Courier ConvertToDal(BO.Courier courier){
        return new DO.Courier(){
            Id = courier.Id,
            Name = courier.FullName,
            Phone = courier.PhoneNumber,
            Active = courier.IsActive,
            Email= courier.Email,
            MaxDeliveryDistance = courier.MaxDistancePreference,
            JoinDate = courier.JoinDate,
            OrderType = (DO.OrderType)courier.Transport,
            Password = courier.Password  // <<< ensure password is preserved on update
        };
    }

    // -------------------------------------------------------
    // ConvertToBO: INCLUDE the Password field so UI can show masked length
    // -------------------------------------------------------
    internal static BO.Courier ConvertToBO(DO.Courier dalCourier)
    {
        if (dalCourier == null)
            throw new BlDoesNotExistException("DAL courier is null.");

        // Read all necessary data from DAL with lock
        List<DO.Delivery> deliveries;
        TimeSpan maxDeliverySpan;
        TimeSpan riskRange;

        lock (AdminManager.BlMutex)
        {
            deliveries = s_dal.Delivery.ReadAll(d => d.CourierId == dalCourier.Id).ToList();
            maxDeliverySpan = s_dal.Config.MaxDeliveryTime;
            riskRange = s_dal.Config.RiskRange;
        }

        // Process data outside the lock
        try
        {
            // basic mapping
            var bo = new BO.Courier
            {
                Id = dalCourier.Id,
                FullName = dalCourier.Name,
                PhoneNumber = dalCourier.Phone,
                IsActive = dalCourier.Active,
                Email = dalCourier.Email,
                Password = dalCourier.Password ?? string.Empty,
                MaxDistancePreference = dalCourier.MaxDeliveryDistance ?? 0.0,
                JoinDate = dalCourier.JoinDate,
                Transport = (BO.Transportation)dalCourier.OrderType
            };

            // --- 2. compute counts: on-time vs late (only for completed deliveries) ---
            int onTime = 0, late = 0;

            var completedDeliveries = deliveries
                .Where(d => d.EndOfOrder == DO.EndOfOrder.Completed && d.TimeOfDelivery.HasValue)
                .ToList();

            if (completedDeliveries.Count > 0)
            {
                var orderIds = completedDeliveries.Select(d => d.OrderId).Distinct().ToList();

                // Read all related orders with lock
                Dictionary<int, DO.Order?> ordersMap;
                lock (AdminManager.BlMutex)
                {
                    ordersMap = orderIds
                        .Select(id => new { id, order = s_dal.Order.Read(id) })
                        .ToDictionary(x => x.id, x => x.order);
                }

                foreach (var del in completedDeliveries)
                {
                    if (!ordersMap.TryGetValue(del.OrderId, out var doOrder) || doOrder == null)
                        continue;

                    DateTime maxAllowed = doOrder.CreatedAt.Add(maxDeliverySpan);

                    if (del.TimeOfDelivery.Value <= maxAllowed)
                        onTime++;
                    else
                        late++;
                }
            }

            // --- 3. find active delivery ---
            var activeDelivery = deliveries
                .Where(d => d.EndOfOrder == null)
                .OrderByDescending(d => d.StartOfDelivery)
                .FirstOrDefault();

            BO.OrderInProgress? activeOrder = null;
            if (activeDelivery is not null)
            {
                BO.Order? boOrder = null;
                try
                {
                    boOrder = OrderManager.Read(activeDelivery.OrderId);
                }
                catch (DO.DalDoesNotExistException) { /* swallow and leave boOrder null */ }

                activeOrder = new BO.OrderInProgress
                {
                    DeliveryId = activeDelivery.Id,
                    OrderId = activeDelivery.OrderId,
                    OrderType = boOrder?.OrderType ?? (BO.OrderTypes)activeDelivery.OrderType,
                    Description = boOrder?.Description ?? string.Empty,
                    Address = boOrder?.FullAddress ?? (boOrder == null ? string.Empty : boOrder.FullAddress),
                    AirDistance = boOrder?.AirDistance ?? 0.0,
                    ActualDistance = activeDelivery.ActualDistance,
                    CustomerName = boOrder?.CustomerName ?? string.Empty,
                    CustomerPhone = boOrder?.CustomerPhone ?? string.Empty,
                    CreatedAt = boOrder?.CreatedAt ?? DateTime.MinValue,
                    StartDeliveryTime = activeDelivery.StartOfDelivery,
                    ExpectedDeliveryTime = (DateTime)boOrder?.ExpectedDeliveryTime,
                    MaxiumDeliveryTime = boOrder?.MaxDeliveryTime ?? DateTime.MinValue,
                    OrderStatus = BO.OrderStatus.InProgress,
                    ScheduleStatus = boOrder?.ScheduleStatus ?? BO.ScheduleStatus.InRisk,
                    TimeLeftForDelivery = boOrder?.RemainingTime ?? TimeSpan.Zero
                };
            }

            bo.DeliveryCountOnTime = onTime;
            bo.DeliveryCountLate = late;
            bo.ActiveOrder = activeOrder;

            return bo;
        }
        catch (DO.DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Related data for courier {dalCourier.Id} not found", ex);
        }
        catch (Exception ex)
        {
            throw new BlFailedToConvert("Failed converting courier from DAL to BO", ex);
        }
    }
    internal static IEnumerable<BO.Courier> ConvertToBOList(IEnumerable<DO.Courier> dalCouriers)
    {
        lock (AdminManager.BlMutex)
            return dalCouriers.Select(dalCourier => ConvertToBO(dalCourier)).ToList();
    }
    internal static bool Exists(int id)
    {
        return ReadAll().Any(c => c.Id == id);
    }

    internal static double GetAverageSpeed(Transportation transport)
    {
        lock (AdminManager.BlMutex)
            return transport switch
        {
            Transportation.Car => s_dal.Config.AverageCarSpeed,
            Transportation.Motorcycle => s_dal.Config.AverageMotorcycleSpeed,
            Transportation.Bike => s_dal.Config.AverageBikeSpeed,
            Transportation.Walking => s_dal.Config.AverageWalkingSpeed,
            _ => throw new BlInvalidValueException("Unknown transportation type.")
        };
    }

    internal static async Task SimulateInactiveCouriersAsync() //stage 7
    {
        // Check and prevent double entry into the method
        if (s_simulationMutex.CheckAndSetInProgress())
            return;

        // Lists to save IDs for observer updates
        List<int> couriersChanged = new();
        List<int> ordersChanged = new();

        try
        {
            // REMOVED: await Task.Run(...) - Not needed, we are already on a background thread

            List<DO.Courier> availableCouriers;
            List<DO.Order> pendingOrders;

            // Fetching data under lock
            lock (AdminManager.BlMutex)
            {
                // 1. Fetch all Deliveries to understand the current state
                var allDeliveries = s_dal.Delivery.ReadAll();

                // 2. Find Busy Couriers (Couriers with active deliveries)
                var busyCourierIds = allDeliveries
                    .Where(d => d.EndOfOrder == null)
                    .Select(d => d.CourierId)
                    .ToHashSet();

                // 3. Find Taken Orders (Orders already assigned/in delivery)
                var takenOrderIds = allDeliveries
                    .Select(d => d.OrderId)
                    .ToHashSet();

                // 4. Get Available Couriers
                availableCouriers = s_dal.Courier.ReadAll()
                    .Where(c => !busyCourierIds.Contains(c.Id))
                    .ToList();

                // 5. Get Pending Orders
                pendingOrders = s_dal.Order.ReadAll()
                    .Where(o => !takenOrderIds.Contains(o.Id))
                    .ToList();
            }

            // Simulation Loop
            foreach (var courier in availableCouriers)
            {
                // If no orders left, stop
                if (!pendingOrders.Any()) break;

                // 50% chance to assign an order
                if (s_rand.NextDouble() < 0.5)
                {
                    DO.Order orderToAssign;

                    lock (AdminManager.BlMutex)
                    {
                        // Double check if orders are still available (safe guard)
                        if (!pendingOrders.Any()) break;

                        orderToAssign = pendingOrders.First();

                        var newDelivery = new DO.Delivery
                        {
                            CourierId = courier.Id,
                            OrderId = orderToAssign.Id,
                            StartOfDelivery = s_dal.Config.Clock, // EXCELLENT CHANGE
                            EndOfOrder = null // Active delivery
                        };

                        s_dal.Delivery.Create(newDelivery);
                    }

                    pendingOrders.Remove(orderToAssign);
                    couriersChanged.Add(courier.Id);
                    ordersChanged.Add(orderToAssign.Id);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error if needed
        }
        finally
        {
            s_simulationMutex.UnsetInProgress();
        }

        // Notify observers outside the lock
        if (couriersChanged.Any())
        {
            foreach (var id in couriersChanged) Observers.NotifyItemUpdated(id);
            Observers.NotifyListUpdated();
        }

        if (ordersChanged.Any())
        {
            foreach (var id in ordersChanged) OrderManager.Observers.NotifyItemUpdated(id);
            OrderManager.Observers.NotifyListUpdated();
        }
    }
}
