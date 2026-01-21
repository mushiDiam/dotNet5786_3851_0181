namespace BlImplementation;

using System;
using BlApi;
using BO;
using Helpers;
using System.Threading.Tasks;

internal class AdminImplementation : IAdmin
{
    /// <summary>
    /// Advances the system clock by a specified time unit.
    /// This is useful for testing and simulation purposes.
    /// </summary>
    /// <param name="T">The time unit to advance (Minute, Hour, Day, Month, or Year)</param>
    public void ForwardClock(Times T){
        // Prevent clock changes while simulator is running to maintain consistency
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        DateTime newTime = AdminManager.Now;
        
        // Determine how much to advance based on the time unit specified
        switch (T){
            case Times.Minute:
                newTime= newTime.AddMinutes(1);
                break;
            case Times.Hour:
                newTime = newTime.AddHours(1);
                break;
            case Times.Day:
                newTime = newTime.AddDays(1);
                break;
            case Times.Month:
                newTime = newTime.AddMonths(1);
                break;
            case Times.Year:
                newTime = newTime.AddYears(1);
                break;
            default:
                break; // No change for unrecognized time unit
        }
        // Apply the new time to the system
        AdminManager.UpdateClock(newTime);
    }

    /// <summary>
    /// Retrieves the current system clock time.
    /// </summary>
    /// <returns>The current DateTime value of the system clock</returns>
    public DateTime GetClock(){
        return AdminManager.Now;
    }

    /// <summary>
    /// Initializes the database with default/sample data.
    /// First resets the database to ensure a clean state, then populates it.
    /// </summary>
    public void InitializeDB(){
        // Prevent initialization while simulator is running
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        ResetDB(); // Clear existing data first
        AdminManager.InitializeDB(); // Populate with initial data
    }

    /// <summary>
    /// Resets the database by clearing all data and restoring default configuration.
    /// </summary>
    public void ResetDB(){
        // Prevent reset while simulator is running to avoid data corruption
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        AdminManager.ResetDB();
    }

    /// <summary>
    /// Retrieves the current system configuration.
    /// </summary>
    /// <returns>A Config object containing all configuration settings</returns>
    public BO.Config GetConfig() => AdminManager.GetConfig();
    
    /// <summary>
    /// Updates the system configuration with new settings.
    /// </summary>
    /// <param name="configuration">The new configuration to apply</param>
    public void SetConfig(BO.Config configuration) {
        // Prevent configuration changes while simulator is running
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        AdminManager.SetConfig(configuration);
    }

    #region Stage 5 - Observer Pattern Implementation
    // These methods implement the Observer pattern for clock and configuration updates.
    // Observers are notified when the clock or configuration changes.
    
    /// <summary>
    /// Registers an observer to be notified when the clock is updated.
    /// </summary>
    /// <param name="clockObserver">The action to invoke when clock changes</param>
    public void AddClockObserver(Action clockObserver) =>
    AdminManager.ClockUpdatedObservers += clockObserver;
    
    /// <summary>
    /// Unregisters a clock observer.
    /// </summary>
    /// <param name="clockObserver">The action to remove from notifications</param>
    public void RemoveClockObserver(Action clockObserver) =>
    AdminManager.ClockUpdatedObservers -= clockObserver;
    
    /// <summary>
    /// Registers an observer to be notified when configuration is updated.
    /// </summary>
    /// <param name="configObserver">The action to invoke when configuration changes</param>
    public void AddConfigObserver(Action configObserver) =>
   AdminManager.ConfigUpdatedObservers += configObserver;
    
    /// <summary>
    /// Unregisters a configuration observer.
    /// </summary>
    /// <param name="configObserver">The action to remove from notifications</param>
    public void RemoveConfigObserver(Action configObserver) =>
    AdminManager.ConfigUpdatedObservers -= configObserver;
    #endregion Stage 5

    /// <summary>
    /// Converts a street address to geographic coordinates using geocoding.
    /// Delegates to the OrderManager to perform the actual API call.
    /// </summary>
    /// <param name="address">The street address to geocode</param>
    /// <returns>A tuple containing the latitude and longitude (nullable if geocoding fails)</returns>
    public async Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address)
        => await OrderManager.GetCoordinatesFromAddressAsync(address);

    #region stage 7 - Simulator Control
    // These methods control the delivery simulation system, which automatically
    // advances time and processes deliveries for testing purposes.
    
    /// <summary>
    /// Starts the delivery simulator with a specified interval.
    /// </summary>
    /// <param name="interval">The time interval (in milliseconds) between simulation steps</param>
    public void StartSimulator(int interval)
    {
        // Ensure simulator isn't already running before starting
        AdminManager.ThrowOnSimulatorIsRunning();
        AdminManager.Start(interval);
    }

    /// <summary>
    /// Stops the currently running simulator.
    /// </summary>
    public void StopSimulator() => AdminManager.Stop();

    /// <summary>
    /// Checks if the simulator is currently running.
    /// </summary>
    /// <returns>True if the simulator is active, false otherwise</returns>
    public bool IsSimulatorRunning() => AdminManager.IsSimulatorRunning();

    /// <summary>
    /// Registers an observer to be notified of simulator state changes.
    /// </summary>
    /// <param name="simulatorObserver">The action to invoke when simulator state changes</param>
    public void AddSimulatorObserver(Action simulatorObserver) =>
        AdminManager.SimulatorUpdatedObservers += simulatorObserver;

    /// <summary>
    /// Unregisters a simulator observer.
    /// </summary>
    /// <param name="simulatorObserver">The action to remove from notifications</param>
    public void RemoveSimulatorObserver(Action simulatorObserver) =>
        AdminManager.SimulatorUpdatedObservers -= simulatorObserver;

    #endregion stage 7
}
