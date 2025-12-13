namespace BlImplementation;

using System;
using BlApi;
using BO;
using Helpers;
using DO;

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
}
