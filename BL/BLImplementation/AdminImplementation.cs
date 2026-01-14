namespace BlImplementation;

using System;
using BlApi;
using BO;
using Helpers;
using System.Threading.Tasks;

internal class AdminImplementation : IAdmin
{
    public void ForwardClock(Times T){
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
        ResetDB();
        AdminManager.InitializeDB();
    }

    public void ResetDB(){
        AdminManager.ResetDB();
    }

    public BO.Config GetConfig() => AdminManager.GetConfig();
    public void SetConfig(BO.Config configuration) => AdminManager.SetConfig(configuration);

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
}
