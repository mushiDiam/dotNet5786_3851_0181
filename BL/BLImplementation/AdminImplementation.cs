namespace BlImplementation;

using System;
using BlApi;
using BO;
using Helpers;
using System.Threading.Tasks;

internal class AdminImplementation : IAdmin
{
    public void ForwardClock(Times T){
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        DateTime newTime = AdminManager.Now;
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
                break;
        }
        AdminManager.UpdateClock(newTime);
    }

    public DateTime GetClock(){
        return AdminManager.Now;
    }

    public void InitializeDB(){
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        ResetDB();
        AdminManager.InitializeDB();
    }

    public void ResetDB(){
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        AdminManager.ResetDB();
    }

    public BO.Config GetConfig() => AdminManager.GetConfig();
    public void SetConfig(BO.Config configuration) {
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        AdminManager.SetConfig(configuration);
    }

    #region Stage 5
    public void AddClockObserver(Action clockObserver) =>
    AdminManager.ClockUpdatedObservers += clockObserver;
    public void RemoveClockObserver(Action clockObserver) =>
    AdminManager.ClockUpdatedObservers -= clockObserver;
    public void AddConfigObserver(Action configObserver) =>
   AdminManager.ConfigUpdatedObservers += configObserver;
    public void RemoveConfigObserver(Action configObserver) =>
    AdminManager.ConfigUpdatedObservers -= configObserver;
    #endregion Stage 5

    // Delegate the geocoding request to BL helper (no PL-level HTTP/json logic).
    public async Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address)
        => await OrderManager.GetCoordinatesFromAddressAsync(address);

    #region stage 7
    public void StartSimulator(int interval)
    {
        AdminManager.ThrowOnSimulatorIsRunning();
        AdminManager.Start(interval);
    }

    public void StopSimulator() => AdminManager.Stop();

    public bool IsSimulatorRunning() => AdminManager.IsSimulatorRunning();

    public void AddSimulatorObserver(Action simulatorObserver) =>
        AdminManager.SimulatorUpdatedObservers += simulatorObserver;

    public void RemoveSimulatorObserver(Action simulatorObserver) =>
        AdminManager.SimulatorUpdatedObservers -= simulatorObserver;

    #endregion stage 7
}
