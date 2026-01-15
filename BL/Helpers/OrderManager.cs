using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using BlApi;
using BlImplementation;
using BO;
using DalApi;
using DO;
using System.Collections.Concurrent;
namespace Helpers;

internal static class OrderManager{

    private static readonly AsyncMutex s_periodicMutex = new(); //stage 7

    private static IDal s_dal = DalApi.Factory.Get;

    internal static ObserverManager Observers = new();

    private static readonly HttpClient s_client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10) // Set global timeout here
    };
    static OrderManager()
    {
        // OSRM requires a User-Agent. Setting it here ensures it's always present.
        s_client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StudentProject/1.0");
    }
    private record DistanceKey(double SrcLat, double SrcLon, double DstLat, double DstLon, BO.Transportation Mode);
    private static readonly ConcurrentDictionary<DistanceKey, double> s_distanceCache = new();
    public static BO.Order ConvertToBO(DO.Order doOrder)
    {
        lock (AdminManager.BlMutex)
        {
            // 1. Calculate Expected Delivery Time (ETA) based on Pickup Time
            DateTime? expectedTime = null;
            try
            {
                // Try to fetch the active delivery for this order
                DO.Delivery? activeDelivery = DeliveryManager.GetDelivery(doOrder.Id);

                // If a courier has picked it up (StartOfDelivery exists) and it's not finished
                if (activeDelivery != null && activeDelivery.StartOfDelivery != default)
                {
                    // Calculate pure travel duration (Distance / Speed)
                    TimeSpan travelDuration = CalculateRemainingTime(doOrder.Id);

                    // ETA = Pickup Time + Travel Duration + 5 minutes buffer (for parking/pickup)
                    expectedTime = activeDelivery.StartOfDelivery.Add(travelDuration).Add(TimeSpan.FromMinutes(5));
                }
            }
            catch (BlDoesNotExistException)
            {
                // No delivery found -> ETA remains null (or you could set it to MaxDeliveryTime)
                expectedTime = null;
            }

            BO.Order boOrder = new BO.Order
            {
                Id = doOrder.Id,
                OrderType = (BO.OrderTypes)doOrder.OrderType,
                FullAddress = doOrder.Address,
                CustomerName = doOrder.CustomerName,
                CustomerPhone = doOrder.CustomerPhone,
                Description = doOrder.Description,
                Latitude = doOrder.Latitude,
                Longitude = doOrder.Longitude,
                // Use existing config for company coords
                AirDistance = GetAirDistance(doOrder.Latitude, doOrder.Longitude, (double)s_dal.Config.CompanyLatitude, (double)s_dal.Config.CompanyLongitude),
                Weight = doOrder.Weight,
                Volume = doOrder.Volume,
                Fragile = doOrder.Fragile,
                CreatedAt = doOrder.CreatedAt,

                // --- NEW ETA CALCULATION ---
                ExpectedDeliveryTime = expectedTime,
                // ---------------------------

                MaxDeliveryTime = doOrder.CreatedAt.Add(s_dal.Config.MaxDeliveryTime),
                OrderStatus = CalculateOrderStatus(doOrder.Id),
                ScheduleStatus = CalculateScheduleStatus(doOrder.Id, doOrder.CreatedAt),
                RemainingTime = CalculateRemainingTime(doOrder.Id),
                Deliveries = GetAllDeliveriesForOrder(doOrder.Id)
            };

            return boOrder;
        }
    }
    public static DO.Order ConvertToDal(BO.Order boOrder)
    {
        DO.Order doOrder = new DO.Order
        {
            Id = boOrder.Id,
            OrderType = (DO.OrderType)boOrder.OrderType,
            Latitude = boOrder.Latitude,
            Longitude = boOrder.Longitude,
            Weight = boOrder.Weight,
            Volume = boOrder.Volume,
            Fragile = boOrder.Fragile,
            CreatedAt = boOrder.CreatedAt,
            CustomerName = boOrder.CustomerName,
            CustomerPhone = boOrder.CustomerPhone,
            Address = boOrder.FullAddress,
            Description = boOrder.Description,
        };
        return doOrder;
    }
    public static void CancelOrder(int orderId)
    {

        // 1. Get the Order (assuming Read throws if not found, based on critique)
        BO.Order boOrder = Read(orderId) ?? throw new BlDoesNotExistException($"Order with ID {orderId} does not exist.");

        // 2. Validate Status
        if (boOrder.OrderStatus == OrderStatus.Closed || boOrder.OrderStatus == OrderStatus.Denied || boOrder.OrderStatus == OrderStatus.Cancelled)
        {
            throw new BlInvalidOperationException("Cannot cancel an order that is already delivered or canceled");
        }

        // 3. Delegate Action to DeliveryManager
        try
        {
            if (boOrder.OrderStatus == OrderStatus.Open)
            {
                // Pass simple data types, not BO objects if possible
                DeliveryManager.CreateMockDeliveryForCancellation(orderId, (DO.OrderType)boOrder.OrderType);
            }
            else if (boOrder.OrderStatus == OrderStatus.InProgress)
            {
                DeliveryManager.CancelActiveDelivery(orderId);
            }
        }
        catch (DO.DalDoesNotExistException ex)
        {
            // Translate DAL errors to BL errors
            throw new BlDoesNotExistException($"Active delivery for order {orderId} not found", ex);
        }
        Observers.NotifyItemUpdated(orderId); //stage 5
        Observers.NotifyListUpdated(); //stage 5

    }

    // Map DAL EndOfOrder to BO.OrderStatus correctly (explicit mapping).
    private static BO.OrderStatus? MapEndOfOrderToBO(DO.EndOfOrder? end)
    {
        if (!end.HasValue) return null;

        return end.Value switch
        {
            DO.EndOfOrder.Completed => BO.OrderStatus.Closed,
            DO.EndOfOrder.Canceled  => BO.OrderStatus.Cancelled,
            DO.EndOfOrder.Denied    => BO.OrderStatus.Denied,
            DO.EndOfOrder.Unreached => BO.OrderStatus.Denied,
            DO.EndOfOrder.Failed    => BO.OrderStatus.Denied,
            _ => null
        };
    }

    // --- update CalculateOrderStatus to use the DAL EndOfOrder explicitly ---
    public static OrderStatus CalculateOrderStatus(int orderId)
    {
        DO.Delivery? del = DeliveryManager.GetDelivery(orderId);
        if (del == null)
        {
            Debug.WriteLine($"Order {orderId} has no delivery record; treating as Open.");
            return OrderStatus.Open;
        }

        // Map DAL EndOfOrder to BO.OrderStatus explicitly
        if (del.EndOfOrder == DO.EndOfOrder.Completed)
            return OrderStatus.Closed;
        if (del.EndOfOrder == DO.EndOfOrder.Canceled)
            return OrderStatus.Cancelled;

        // Treat Denied/Unreached/Failed as Denied in BO
        if (del.EndOfOrder == DO.EndOfOrder.Denied ||
            del.EndOfOrder == DO.EndOfOrder.Unreached ||
            del.EndOfOrder == DO.EndOfOrder.Failed)
            return OrderStatus.Denied;

        // Otherwise delivery exists but not finished
        return OrderStatus.InProgress;
    }

    /// <summary>
    /// Calculate schedule status (On time / Over time / In risk) using delivery timings and configured risk window.
    /// </summary>
    /// <param name="orderId">Order id.</param>
    /// <returns><see cref="ScheduleStatus"/> for the order.</returns>
    private static ScheduleStatus CalculateScheduleStatus(int orderId, DateTime OrderTime)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Delivery? del = DeliveryManager.GetDelivery(orderId);
            DateTime maxDeliveryTime = OrderTime.Add(s_dal.Config.MaxDeliveryTime);
            TimeSpan riskRange = s_dal.Config.RiskRange;

            // 1. Order has no delivery yet (Open)
            if (del == null)
            {
                // FIX: Use s_dal.Config.Clock instead of DateTime.Now
                if (s_dal.Config.Clock > maxDeliveryTime)
                    return ScheduleStatus.Late;

                var remainingToMax = maxDeliveryTime - s_dal.Config.Clock;
                return remainingToMax <= riskRange ? ScheduleStatus.InRisk : ScheduleStatus.OnTime;
            }

            // 2. Delivery Completed
            if (del.EndOfOrder == DO.EndOfOrder.Completed)
            {
                if (del.TimeOfDelivery.HasValue && del.TimeOfDelivery.Value <= maxDeliveryTime)
                    return ScheduleStatus.OnTime;
                return ScheduleStatus.Late;
            }

            // 3. Delivery In Progress
            // FIX: Use s_dal.Config.Clock instead of DateTime.Now
            if (s_dal.Config.Clock > maxDeliveryTime)
                return ScheduleStatus.Late;

            var remaining = maxDeliveryTime - s_dal.Config.Clock;
            return remaining <= riskRange ? ScheduleStatus.InRisk : ScheduleStatus.OnTime;
        }
    }
    private static double GetAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371; // Radius of the earth in km
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c; // Distance in km
        return distance;
    }
    private static double ToRadians(double deg)
    {
        return deg * (Math.PI / 180);
    }
    private static TimeSpan CalculateRemainingTime(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Delivery? del = DeliveryManager.GetDelivery(orderId);

            // 1. If no delivery, no remaining time estimate
            if (del == null) return TimeSpan.Zero;

            // 2. If finished, remaining time is zero
            if (del.EndOfOrder == DO.EndOfOrder.Completed) return TimeSpan.Zero;

            // 3. If In Progress (Started)
            if (del.StartOfDelivery != default)
            {
                // Calculate total travel duration (Distance / Speed)
                DO.OrderType shiftType = del.OrderType;
                double delSpeed = shiftType switch
                {
                    DO.OrderType.Car => s_dal.Config.AverageCarSpeed,
                    DO.OrderType.Motorcycle => s_dal.Config.AverageMotorcycleSpeed,
                    DO.OrderType.Bike => s_dal.Config.AverageBikeSpeed,
                    DO.OrderType.Walking => s_dal.Config.AverageWalkingSpeed,
                    _ => 1 // avoid division by zero
                };

                double distance = del.ActualDistance ?? 0;
                double hours = distance / delSpeed;

                // Total expected duration (+ 5 mins buffer)
                TimeSpan totalDuration = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(5));

                // Calculate exact ETA
                DateTime eta = del.StartOfDelivery.Add(totalDuration);

                // Remaining = ETA - Current Simulator Time
                TimeSpan remaining = eta - s_dal.Config.Clock;

                // Return the remaining time (ensure it doesn't show negative)
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }

            return TimeSpan.Zero;
        }
    }
    // In OrderManager.cs (in the Helpers folder)
    internal static void MarkDeliveryNotFound(int courierId, int deliveryId)
    {
        int orderId;
        int actualCourierId;

        lock (AdminManager.BlMutex)
        {
            DO.Delivery delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
            if (delivery == null)
                throw new BlDoesNotExistException($"Delivery {deliveryId} not found");
            if (delivery.CourierId != courierId)
                throw new BlUnauthorizedAccessException("Courier can only mark their own deliveries");

            // Store values needed for observer notifications
            orderId = delivery.OrderId;
            actualCourierId = delivery.CourierId;

            // Remove the delivery record so the order becomes available (Open) again.
            // Deleting preserves the intended behavior: CalculateOrderStatus will treat orders
            // with no delivery record as Open.
            try
            {
                s_dal.Delivery.Delete(deliveryId);
            }
            catch (DalDoesNotExistException ex)
            {
                throw new BlDoesNotExistException($"Delivery {deliveryId} not found", ex);
            }
        }

        // Notify observers outside the lock
        // Notify delivery observers so PL can update lists/details.
        try { DeliveryManager.Observers.NotifyListUpdated(); } catch { }
        try { DeliveryManager.Observers.NotifyItemUpdated(deliveryId); } catch { }
        // Notify order observers (by order id) so order status becomes Open in PL.
        try { Observers.NotifyItemUpdated(orderId); } catch { }
        try { Observers.NotifyListUpdated(); } catch { }
        // Notify courier observers (so courier detail UI updates)
        try { if (actualCourierId != 0) CourierManager.Observers.NotifyItemUpdated(actualCourierId); } catch { }
    }
    /// <summary>
    /// Return all deliveries (from the DAL) for the specified order id converted to
    /// <see cref="DeliveryPerOrderInList"/> instances for presentation/listing.
    /// </summary>
    /// <param name="orderId">Order id to query.</param>
    /// <returns>List of <see cref="DeliveryPerOrderInList"/> for the order.</returns>
    private static List<DeliveryPerOrderInList> GetAllDeliveriesForOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            // --- update GetAllDeliveriesForOrder mapping to set Delivery.OrderStatus using mapper ---
            var deliveries = s_dal.Delivery.ReadAll(d => d.OrderId == orderId)
            .Select(d =>
            {
                var courier = s_dal.Courier.Read(c => c.Id == d.CourierId);
                return new DeliveryPerOrderInList
                {
                    DeliveryId = d.Id,
                    CourierId = d.CourierId,
                    CourierName = courier?.Name ?? string.Empty,
                    Transport = (BO.Transportation)d.OrderType,
                    // map DAL EndOfOrder to BO.OrderStatus (nullable)
                    OrderStatus = MapEndOfOrderToBO(d.EndOfOrder),
                    EndTime = d.TimeOfDelivery,
                    StartTime = d.StartOfDelivery,
                };
            })
            .ToList();

            return deliveries;
        }
    }
    // In OrderManager.cs (Internal Static Class)

    /// <summary>
    /// Validates logic, calculates internal metrics (Distance), and saves to DAL.
    /// Note: This method is Synchronous. The Async network coordinates resolution 
    /// MUST happen in OrderImplementation before calling this.
    /// </summary>
    internal static void CreateOrder(BO.Order order)
    {
        // 1. Validation: Ensure object is not null
        if (order is null)
            throw new BlInvalidValueException("Order cannot be null.");

        // 2. Validation: Ensure address string exists
        if (string.IsNullOrWhiteSpace(order.FullAddress))
            throw new BlInvalidValueException("Order address is required.");

        // 3. Validation: Verify Coordinates
        // We expect OrderImplementation to have already resolved these via the Network.
        // If they are still NaN or 0, it means the async part failed or wasn't called.
        if (double.IsNaN(order.Latitude) || double.IsNaN(order.Longitude))
            throw new BlInvalidValueException("Coordinates missing (Async resolution failed).");

        // 4. Calculate Air Distance (CPU-bound calculation, safe to be synchronous)
        // Fetch company coordinates from DAL Configuration
        lock (AdminManager.BlMutex)
        {
            double? companyLat = s_dal.Config.CompanyLatitude;
            double? companyLon = s_dal.Config.CompanyLongitude;

            if (companyLat != null && companyLon != null)
            {
                order.AirDistance = GetAirDistance(order.Latitude, order.Longitude, companyLat.Value, companyLon.Value);
            }
        }

        // 5. Convert BO entity to DO (DAL) entity
        // (Ensure you have this mapping logic either here or in an extension method)
        DO.Order doOrder = new DO.Order
        {
            // If ID is 0, DAL usually auto-increments it
            Id = order.Id,
            CustomerName = order.CustomerName,
            Address = order.FullAddress,
            CustomerPhone = order.CustomerPhone, // Assuming this field exists
            Latitude = order.Latitude,
            Longitude = order.Longitude,
            Weight = order.Weight,
            Volume = order.Volume,
            Fragile = order.Fragile,
            // Cast Enum if necessary
            OrderType = (DO.OrderType)order.OrderType,
            CreatedAt = order.CreatedAt == default ? DateTime.Now : order.CreatedAt,
            Description = order.Description
        };

        // 6. Persist to Data Layer
        lock (AdminManager.BlMutex)
        {
            try
            {
                // Dal.Order.Add returns the new ID (int)
                s_dal.Order.Create(doOrder);

                // Update the BO object with the new ID generated by DAL
                if (doOrder.Id != 0)
                {
                    order.Id = doOrder.Id;
                }
                else
                {
                    // Option B: The "Black Box" DAL Fallback
                    // If DAL didn't update our object, we ask the DB: "What is the latest order?"
                    // We assume the one with the highest ID is the one we just added.
                    var allOrders = s_dal.Order.ReadAll();
                    if (allOrders.Any())
                    {
                        // Get the ID of the order created most recently (Max ID)
                        order.Id = allOrders.Max(o => o.Id);
                    }
                }
            }
            catch (DO.DalAlreadyExistsException ex)
            {
                throw new BlAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
            }
        }
        // 7. Notify Observers (Stage 5 Requirement)
        // This updates the PL windows listening to list changes
        Observers.NotifyListUpdated();
    }
    internal static BO.Order? Read(int orderId)
    {
        DO.Order doOrder;
        try
        {
            lock (AdminManager.BlMutex)
                doOrder = s_dal.Order.Read(orderId);
        }
        catch(DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {orderId} does not exist.", ex);
        }
        if (doOrder is null)
            return null;
        return ConvertToBO(doOrder);
    }
    internal static void Update(BO.Order order)
    {
        if (order is null) throw new BlInvalidValueException("Order cannot be null.");

        // 1. Get Existing Order
        DO.Order existingDoOrder;
        try
        {
            lock (AdminManager.BlMutex)
                existingDoOrder = s_dal.Order.Read(order.Id);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
        }

        // 2. Status Check
        var currentStatus = CalculateOrderStatus(order.Id);
        if (currentStatus == OrderStatus.Closed || currentStatus == OrderStatus.Denied || currentStatus == OrderStatus.Cancelled)
            throw new BlInvalidOperationException("Cannot update a closed order.");

        // 3. Merge Logic (With Data Integrity Check)
        // If input address is valid and DIFFERENT from existing, we MUST use input coordinates.
        // We cannot fallback to existing coordinates if the address changed.

        bool addressChanged = !string.IsNullOrWhiteSpace(order.FullAddress) &&
                              !order.FullAddress.Equals(existingDoOrder.Address); // Check spelling of Address property in DO

        double newLat, newLon;

        if (addressChanged)
        {
            // If address changed, we REQUIRE valid new coordinates from the input
            if (double.IsNaN(order.Latitude) || double.IsNaN(order.Longitude))
                throw new BlInvalidValueException("Address changed but coordinates are missing.");

            newLat = order.Latitude;
            newLon = order.Longitude;
        }
        else
        {
            // Address didn't change, so we keep existing coordinates (safe)
            newLat = existingDoOrder.Latitude;
            newLon = existingDoOrder.Longitude;
        }

        // 4. Update the DO Record
        var updatedDoOrder = existingDoOrder with
        {
            Address = string.IsNullOrWhiteSpace(order.FullAddress) ? existingDoOrder.Address : order.FullAddress,
            CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? existingDoOrder.CustomerName : order.CustomerName,
            CustomerPhone = string.IsNullOrWhiteSpace(order.CustomerPhone) ? existingDoOrder.CustomerPhone : order.CustomerPhone,
            Description = order.Description ?? existingDoOrder.Description,
            Weight = order.Weight,
            Volume = order.Volume,
            Fragile = order.Fragile,
            OrderType = (DO.OrderType)order.OrderType,
            Latitude = newLat,
            Longitude = newLon
        };

        // 5. Save
        lock (AdminManager.BlMutex)
        {
            try
            {
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (DalDoesNotExistException ex)
            {
                throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
            }
        }
        Observers.NotifyItemUpdated(order.Id);
        Observers.NotifyListUpdated();
    }
    internal static void Delete(int orderId)
    {
        try
        {
            lock (AdminManager.BlMutex)
                s_dal.Order.Delete(orderId);
        }
        catch (DalDoesNotExistException exception)
        {
            throw new BlInvalidOperationException($"Order ID {orderId} does not exist.", exception);
        }
        Observers.NotifyItemUpdated(orderId); //stage 5
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            var doOrders = s_dal.Order.ReadAll();
            var boOrders = doOrders.Select(doOrder => ConvertToBO(doOrder));
            if (filter != null)
            {
                boOrders = boOrders.Where(filter);
            }
            return boOrders;
        }
    }
    internal static void DeleteAll()
    {
        lock (AdminManager.BlMutex)
            s_dal.Order.DeleteAll();
        Observers.NotifyListUpdated(); //stage 5
    }
    public static async Task<double?> GetActualDistanceAsync(double latitude, double longitude, BO.Transportation transport)
    {
        double? companyLat;
        double? companyLon;

        // --- DAL Access with Lock ---
        lock (AdminManager.BlMutex)
        {
            companyLat = s_dal.Config.CompanyLatitude;
            companyLon = s_dal.Config.CompanyLongitude;
        }

        // --- Validation ---
        if (companyLat == null || companyLon == null)
            throw new BlInvalidValueException("Company coordinates are not configured.");
        if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
            latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new BlInvalidValueException("Order coordinates are invalid.");

        // --- Cache Check ---
        var key = NormalizeKey(latitude, longitude, companyLat.Value, companyLon.Value, transport);
        if (s_distanceCache.TryGetValue(key, out double cachedDistance))
        {
            return cachedDistance;
        }

        // --- Profile Mapping ---
        string profile = transport switch
        {
            BO.Transportation.Car => "car",
            BO.Transportation.Motorcycle => "car",
            BO.Transportation.Bike => "bike",
            BO.Transportation.Walking => "foot",
            _ => throw new BlInvalidValueException("Invalid transportation type")
        };

        // --- URL Construction (using local variables) ---
        string strLon1 = companyLon.Value.ToString(CultureInfo.InvariantCulture);
        string strLat1 = companyLat.Value.ToString(CultureInfo.InvariantCulture);
        string strLon2 = longitude.ToString(CultureInfo.InvariantCulture);
        string strLat2 = latitude.ToString(CultureInfo.InvariantCulture);
        string coordinates = $"{strLon1},{strLat1};{strLon2},{strLat2}";
        string url = $"https://router.project-osrm.org/route/v1/{profile}/{coordinates}?overview=false";

        try
        {
            string json = await s_client.GetStringAsync(url);
            using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeProp) && codeProp.GetString() != "Ok")
                return null;
            var routes = root.GetProperty("routes");
            if (routes.GetArrayLength() == 0) return null;
            double meters = routes[0].GetProperty("distance").GetDouble();
            double km = Math.Round(meters / 1000.0, 2);
            // Cache Update
            s_distanceCache.TryAdd(key, km);
            return km;
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
        catch (TaskCanceledException) { return null; }
    }
    public static async Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return (null, null);

        string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

        try
        {
            if (!s_client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                s_client.DefaultRequestHeaders.Add("User-Agent", "CouriersApp/1.0 (myemail@example.com)");
            }

            string json = await s_client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetArrayLength() > 0)
            {
                var location = root[0];
                double lat = double.Parse(location.GetProperty("lat").GetString()!);
                double lon = double.Parse(location.GetProperty("lon").GetString()!);
                return (lat, lon);
            }
        }
        catch
        {
        }

        return (null, null);
    }
    private static OrderInList ConvertToOrderInList(BO.Order boOrder)
    {
        TimeSpan completionTime = TimeSpan.Zero;
        DO.Delivery? del = DeliveryManager.GetDelivery(boOrder.Id);
        if (del != null && del.EndOfOrder == DO.EndOfOrder.Completed && del.TimeOfDelivery.HasValue)
        {
            completionTime = del.TimeOfDelivery.Value - boOrder.CreatedAt;
        }
        return new OrderInList
        {
            DeliveryId = del?.Id,
            OrderId = boOrder.Id,
            OrderType = boOrder.OrderType,
            AirDistance = boOrder.AirDistance,
            OrderStatus= boOrder.OrderStatus,
            ScheduleStatus = boOrder.ScheduleStatus,
            RemainingTime = boOrder.RemainingTime,
            CompletionTime = completionTime,
            DeliveryCount = boOrder.Deliveries?.Count ?? 0
        };
    }
    public static IEnumerable<OrderInList> GetOrders(OrderInListOptions? filter, object? obj, OrderInListOptions? sort)
    {
        // 1. DEFERRED EXECUTION
        IEnumerable<BO.Order> boOrders = ReadAll();

        // 2. PRE-LOAD DELIVERIES (The N+1 Fix) - Read with lock, then release
        Dictionary<int, List<DO.Delivery>> allDeliveries;
        lock (AdminManager.BlMutex)
        {
            allDeliveries = s_dal.Delivery.ReadAll()
                .GroupBy(d => d.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // 3. FILTERING (outside lock)
        if (filter.HasValue && obj != null)
        {
            switch (filter)
            {
                case OrderInListOptions.DeliveryId:
                    if (obj is int deliveryId)
                    {
                        boOrders = boOrders.Where(o =>
                            allDeliveries.TryGetValue(o.Id, out var dels) &&
                            dels.Any(d => d.Id == deliveryId)
                        );
                    }
                    else throw new BlInvalidOperationException("Filter value for DeliveryId must be an integer");
                    break;

                case OrderInListOptions.OrderId:
                    if (obj is int orderId)
                        boOrders = boOrders.Where(o => o.Id == orderId);
                    break;

                case OrderInListOptions.OrderType:
                    if (obj is BO.OrderTypes type)
                        boOrders = boOrders.Where(o => o.OrderType == type);
                    break;

                case OrderInListOptions.AirDistance:
                    if (obj is double maxDistance)
                        boOrders = boOrders.Where(o => o.AirDistance <= maxDistance);
                    break;

                case OrderInListOptions.OrderStatus:
                    if (obj is BO.OrderStatus status)
                        boOrders = boOrders.Where(o => o.OrderStatus == status);
                    break;

                case OrderInListOptions.ScheduleStatus:
                    if (obj is BO.ScheduleStatus schedStatus)
                        boOrders = boOrders.Where(o => o.ScheduleStatus == schedStatus);
                    break;

                case OrderInListOptions.RemainingTime:
                    if (obj is TimeSpan maxRemaining)
                        boOrders = boOrders.Where(o => o.RemainingTime <= maxRemaining);
                    break;

                case OrderInListOptions.CompletionTime:
                    if (obj is TimeSpan maxTime)
                    {
                        boOrders = boOrders.Where(o =>
                        {
                            var time = GetCompletionTime(o.Id, o.CreatedAt, allDeliveries);
                            return time != null && time <= maxTime;
                        });
                    }
                    break;

                case OrderInListOptions.DeliveryCount:
                    if (obj is int minCount)
                        boOrders = boOrders.Where(o =>
                            allDeliveries.TryGetValue(o.Id, out var dels) && dels.Count >= minCount
                        );
                    break;

                default:
                    break;
            }
        }

        // 4. SORTING (outside lock)
        if (!sort.HasValue)
        {
            boOrders = boOrders.OrderBy(o => o.OrderStatus);
        }
        else
        {
            switch (sort.Value)
            {
                case OrderInListOptions.DeliveryId:
                    boOrders = boOrders.OrderBy(o =>
                        allDeliveries.TryGetValue(o.Id, out var dels)
                            ? dels.OrderByDescending(d => d.StartOfDelivery).FirstOrDefault()?.Id ?? int.MaxValue
                            : int.MaxValue
                    );
                    break;

                case OrderInListOptions.OrderId:
                    boOrders = boOrders.OrderBy(o => o.Id);
                    break;

                case OrderInListOptions.OrderType:
                    boOrders = boOrders.OrderBy(o => o.OrderType);
                    break;

                case OrderInListOptions.AirDistance:
                    boOrders = boOrders.OrderBy(o => o.AirDistance);
                    break;

                case OrderInListOptions.OrderStatus:
                    boOrders = boOrders.OrderBy(o => o.OrderStatus);
                    break;

                case OrderInListOptions.ScheduleStatus:
                    boOrders = boOrders.OrderBy(o => o.ScheduleStatus);
                    break;

                case OrderInListOptions.RemainingTime:
                    boOrders = boOrders.OrderBy(o => o.RemainingTime);
                    break;

                case OrderInListOptions.CompletionTime:
                    boOrders = boOrders.OrderBy(o =>
                        GetCompletionTime(o.Id, o.CreatedAt, allDeliveries) ?? TimeSpan.MaxValue
                    );
                    break;

                case OrderInListOptions.DeliveryCount:
                    boOrders = boOrders.OrderBy(o =>
                        allDeliveries.TryGetValue(o.Id, out var dels) ? dels.Count : 0
                    );
                    break;

                default:
                    boOrders = boOrders.OrderBy(o => o.OrderStatus);
                    break;
            }
        }

        // 5. PROJECTION (outside lock)
        foreach (var order in boOrders)
        {
            var deliveriesForOrder = allDeliveries.TryGetValue(order.Id, out var dels)
                ? dels
                : new List<DO.Delivery>();

            yield return ConvertToOrderInListCached(order, deliveriesForOrder, allDeliveries);
        }
    }
    private static TimeSpan? GetCompletionTime(int orderId, DateTime createdAt, Dictionary<int, List<DO.Delivery>> cache)
    {
        // Fixes Critique #2: Use TryGetValue
        if (!cache.TryGetValue(orderId, out var dels)) return null;

        var lastCompleted = dels
            .Where(d => d.EndOfOrder == DO.EndOfOrder.Completed && d.TimeOfDelivery.HasValue)
            .OrderByDescending(d => d.TimeOfDelivery)
            .FirstOrDefault();

        return lastCompleted?.TimeOfDelivery!.Value - createdAt;
    }
    internal static OrderInList ConvertToOrderInListCached(BO.Order order, List<DO.Delivery> deliveries, Dictionary<int, List<DO.Delivery>> cache) // Pass cache if needed for helper reuse
    {
        var latestDelivery = deliveries
            .OrderByDescending(d => d.StartOfDelivery)
            .FirstOrDefault();

        // REUSE the logic helper here instead of rewriting it
        // Fixes Critique #3: Logic duplicated -> Logic reused
        TimeSpan completionTime = GetCompletionTime(order.Id, order.CreatedAt, cache) ?? TimeSpan.Zero;

        return new OrderInList
        {
            OrderId = order.Id,
            DeliveryId = latestDelivery?.Id,
            OrderType = order.OrderType,
            AirDistance = order.AirDistance,
            OrderStatus = order.OrderStatus,
            ScheduleStatus = order.ScheduleStatus,
            RemainingTime = order.RemainingTime,
            CompletionTime = completionTime,
            DeliveryCount = deliveries.Count
        };
    }
    internal static void EndDelivery(int courierId, int deliveryId)
    {
        DO.Delivery updatedDelivery;
        lock (AdminManager.BlMutex)
        {
            DO.Delivery delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
            if (delivery == null)
                throw new BlDoesNotExistException($"Delivery {deliveryId} not found");
            updatedDelivery = delivery with
            {
                EndOfOrder = DO.EndOfOrder.Completed,
                TimeOfDelivery = DateTime.Now
            };

            s_dal.Delivery.Update(updatedDelivery);
        }
        // Notify delivery observers (deliveryId)
        DeliveryManager.Observers.NotifyItemUpdated(deliveryId);
        DeliveryManager.Observers.NotifyListUpdated();

        // Notify order observers (orderId)
        Observers.NotifyItemUpdated(updatedDelivery.OrderId);
        Observers.NotifyListUpdated();

        // Notify courier observers (courierId) so courier detail UI updates
        try { if (updatedDelivery.CourierId != 0) CourierManager.Observers.NotifyItemUpdated(updatedDelivery.CourierId); } catch { }
        
    }

    // Option: Normalize key so A→B and B→A use the same cache entry
    private static DistanceKey NormalizeKey(double lat1, double lon1, double lat2, double lon2, BO.Transportation mode)
    {
        // Always put the "smaller" coordinate pair first
        if (lat1 < lat2 || (lat1 == lat2 && lon1 < lon2))
            return new DistanceKey(lat1, lon1, lat2, lon2, mode);
        else
            return new DistanceKey(lat2, lon2, lat1, lon1, mode);
    }
}
