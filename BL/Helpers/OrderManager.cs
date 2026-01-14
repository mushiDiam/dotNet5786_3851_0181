using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using BlApi;
using BlImplementation;
using BO;
using DalApi;
using DO;
namespace Helpers;

internal static class OrderManager{
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
    public static BO.Order ConvertToBO(DO.Order doOrder)
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
            FullAddress = doOrder.AdderssOfOrder,
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
            AdderssOfOrder = boOrder.FullAddress,
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
    // In OrderManager.cs (in the Helpers folder)
    internal static void MarkDeliveryNotFound(int courierId, int deliveryId)
    {
        DO.Delivery delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
        if (delivery == null)
            throw new BlDoesNotExistException($"Delivery {deliveryId} not found");

        if (delivery.CourierId != courierId)
            throw new BlUnauthorizedAccessException("Courier can only mark their own deliveries");

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

        // Notify delivery observers so PL can update lists/details.
        try { DeliveryManager.Observers.NotifyListUpdated(); } catch { }
        try { DeliveryManager.Observers.NotifyItemUpdated(deliveryId); } catch { }

        // Notify order observers (by order id) so order status becomes Open in PL.
        try { Observers.NotifyItemUpdated(delivery.OrderId); } catch { }
        try { Observers.NotifyListUpdated(); } catch { }

        // Notify courier observers (so courier detail UI updates)
        try { if (delivery.CourierId != 0) CourierManager.Observers.NotifyItemUpdated(delivery.CourierId); } catch { }
    }

    /// <summary>
    /// Return all deliveries (from the DAL) for the specified order id converted to
    /// <see cref="DeliveryPerOrderInList"/> instances for presentation/listing.
    /// </summary>
    /// <param name="orderId">Order id to query.</param>
    /// <returns>List of <see cref="DeliveryPerOrderInList"/> for the order.</returns>
    private static List<DeliveryPerOrderInList> GetAllDeliveriesForOrder(int orderId)
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
    internal static async void CreateOrder(BO.Order order)
    {
        if (order is null)
            throw new BlInvalidValueException("Order cannot be null.");

        if (string.IsNullOrWhiteSpace(order.FullAddress))
            throw new BlInvalidValueException("Order address is required.");

        // Resolve coordinates from address if not provided
        if (double.IsNaN(order.Latitude) || double.IsNaN(order.Longitude) || (order.Latitude == 0 && order.Longitude == 0))
        {
            // reuse the geocoding helper already in this class (async) synchronously
            var coords = await GetCoordinatesFromAddressAsync(order.FullAddress);
            if (coords.Lat == null || coords.Lon == null)
                throw new BlInvalidValueException("Unable to resolve address coordinates.");

            order.Latitude = coords.Lat.Value;
            order.Longitude = coords.Lon.Value;
        }

        // Validate coordinates are now set
        if (double.IsNaN(order.Latitude) || double.IsNaN(order.Longitude))
            throw new BlInvalidValueException("Order coordinates are invalid.");

        // compute air distance using company coordinates from DAL config
        var companyLat = s_dal.Config.CompanyLatitude ?? double.NaN;
        var companyLon = s_dal.Config.CompanyLongitude ?? double.NaN;
        if (double.IsNaN(companyLat) || double.IsNaN(companyLon))
            throw new BlInvalidValueException("Company coordinates are not configured.");

        // Use existing helper GetAirDistance (returns km)
        order.AirDistance = GetAirDistance(order.Latitude, order.Longitude, companyLat, companyLon);

        // convert and persist
        DO.Order doOrder = ConvertToDal(order);
        try
        {
            s_dal.Order.Create(doOrder);
        }
        catch (DalAlreadyExistsException ex)
        {
            throw new BlAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
        }
        Observers.NotifyListUpdated(); //stage 5
    }
    internal static BO.Order? Read(int orderId)
    {
        DO.Order doOrder;
        try
        {
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
    internal static async Task Update(BO.Order order)
    {
        if (order is null)
            throw new BlInvalidValueException("Order cannot be null.");

        DO.Order existingDoOrder;
        try
        {
            existingDoOrder = s_dal.Order.Read(order.Id);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
        }

        // Determine current BO order status (Open/InProgress => editable; others => closed)
        var currentStatus = CalculateOrderStatus(order.Id);
        if (currentStatus == OrderStatus.Closed || currentStatus == OrderStatus.Denied || currentStatus == OrderStatus.Cancelled)
            throw new BlInvalidOperationException("Cannot update a closed order.");

        // Whitelist: only these fields may be updated
        // Adjust the list below if business requires different fields
        var updatedDoOrder = existingDoOrder with
        {
            AdderssOfOrder = string.IsNullOrWhiteSpace(order.FullAddress) ? existingDoOrder.AdderssOfOrder : order.FullAddress,
            CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? existingDoOrder.CustomerName : order.CustomerName,
            CustomerPhone = string.IsNullOrWhiteSpace(order.CustomerPhone) ? existingDoOrder.CustomerPhone : order.CustomerPhone,
            Description = order.Description ?? existingDoOrder.Description,
            Weight = order.Weight,
            Volume = order.Volume,
            Fragile = order.Fragile,
            OrderType = (DO.OrderType)order.OrderType,
            Latitude = double.IsNaN(order.Latitude) ? existingDoOrder.Latitude : order.Latitude,
            Longitude = double.IsNaN(order.Longitude) ? existingDoOrder.Longitude : order.Longitude,
            // Keep CreatedAt and Id unchanged
        };

        // If address changed and coordinates are invalid, try resolving coordinates
        if (!string.Equals(existingDoOrder.AdderssOfOrder, updatedDoOrder.AdderssOfOrder, StringComparison.OrdinalIgnoreCase) &&
            (updatedDoOrder.Latitude == 0 && updatedDoOrder.Longitude == 0))
        {
            var coords = await GetCoordinatesFromAddressAsync(updatedDoOrder.AdderssOfOrder);
            if (coords.Lat.HasValue && coords.Lon.HasValue)
            {
                updatedDoOrder = updatedDoOrder with { Latitude = coords.Lat.Value, Longitude = coords.Lon.Value };
            }
        }

        try
        {
            s_dal.Order.Update(updatedDoOrder);
        }
        catch (DalDoesNotExistException ex)
        {
            throw new BlDoesNotExistException($"Order ID {order.Id} does not exist.", ex);
        }

        Observers.NotifyItemUpdated(order.Id);
        Observers.NotifyListUpdated();
    }
    internal static void Delete(int orderId)
    {
        try
        {
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
        var doOrders = s_dal.Order.ReadAll();
        var boOrders = doOrders.Select(doOrder => ConvertToBO(doOrder));
        if (filter != null)
        {
            boOrders = boOrders.Where(filter);
        }
        return boOrders;
    }
    internal static void DeleteAll()
    {
        s_dal.Order.DeleteAll();
        Observers.NotifyListUpdated(); //stage 5
    }
    public static async Task<double?> GetActualDistanceAsync(double latitude, double longitude, BO.Transportation transport)
    {
        // --- Validation (Your code is good here) ---
        if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
            latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new BlInvalidValueException("Order coordinates are invalid.");

        if (double.IsNaN((double)s_dal.Config.CompanyLatitude) || double.IsNaN((double)s_dal.Config.CompanyLongitude))
            throw new BlInvalidValueException("Company coordinates are not configured.");

        // --- Profile Mapping ---
        string profile = transport switch
        {
            BO.Transportation.Car => "car",
            BO.Transportation.Motorcycle => "car",
            BO.Transportation.Bike => "bike",
            BO.Transportation.Walking => "walking",
            _ => throw new BlInvalidValueException("Invalid transportation type")
        };

        // --- URL Construction ---
        string coordinates =
            $"{longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)};" +
            $"{Convert.ToDouble(s_dal.Config.CompanyLongitude).ToString(CultureInfo.InvariantCulture)},{Convert.ToDouble(s_dal.Config.CompanyLatitude).ToString(CultureInfo.InvariantCulture)}";

        string url = $"https://router.project-osrm.org/table/v1/{profile}/{coordinates}?annotations=distance";

        try
        {
            // 3. REUSE the static client (Do NOT use 'using' here)
            string json = await s_client.GetStringAsync(url).ConfigureAwait(false);

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("code", out var codeProp) && codeProp.GetString() != "Ok")
                return null;

            var distances = root.GetProperty("distances");

            if (distances.GetArrayLength() == 0 || distances[0].GetArrayLength() < 2)
                return null;

            double meters = distances[0][1].GetDouble();
            return Math.Round(meters / 1000.0, 2);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            // This catches the Timeout
            return null;
        }
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

        // 2. PRE-LOAD DELIVERIES (The N+1 Fix)
        var allDeliveries = s_dal.Delivery.ReadAll()
            .GroupBy(d => d.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 3. FILTERING
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

        // 4. SORTING
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
                        // Use TryGetValue logic inline or helper if complex
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

        // 5. PROJECTION
        foreach (var order in boOrders)
        {
            // Use TryGetValue efficiently
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
    private static OrderInList ConvertToOrderInListCached(BO.Order order, List<DO.Delivery> deliveries, Dictionary<int, List<DO.Delivery>> cache) // Pass cache if needed for helper reuse
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

        DO.Delivery delivery = s_dal.Delivery.Read(d => d.Id == deliveryId);
        if (delivery == null)
            throw new BlDoesNotExistException($"Delivery {deliveryId} not found");
        DO.Delivery updatedDelivery = delivery with
        {
            EndOfOrder = DO.EndOfOrder.Completed,
            TimeOfDelivery = DateTime.Now
        };

        s_dal.Delivery.Update(updatedDelivery);

        // Notify delivery observers (deliveryId)
        DeliveryManager.Observers.NotifyItemUpdated(deliveryId);
        DeliveryManager.Observers.NotifyListUpdated();

        // Notify order observers (orderId)
        Observers.NotifyItemUpdated(updatedDelivery.OrderId);
        Observers.NotifyListUpdated();

        // Notify courier observers (courierId) so courier detail UI updates
        try { if (updatedDelivery.CourierId != 0) CourierManager.Observers.NotifyItemUpdated(updatedDelivery.CourierId); } catch { }
        
    }
}
