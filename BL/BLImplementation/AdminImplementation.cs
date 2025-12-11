namespace BLImplementation;
using BlApi;
using BO;
using Helpers;
using System;

internal class AdminImplementation : IAdmin
{
    public void ForwardClock(Times T)
    {
        switch (T)
        {
            case Times.Minute:
                AdminManager.UpdateClock(AdminManager.Now.AddMinutes(1));
                break;
            case Times.Hour:
                AdminManager.UpdateClock(AdminManager.Now.AddHours(1));
                break;
            case Times.Day:
                AdminManager.UpdateClock(AdminManager.Now.AddDays(1));
                break;
            case Times.Month:
                AdminManager.UpdateClock(AdminManager.Now.AddMonths(1));
                break;
            case Times.Year:
                AdminManager.UpdateClock(AdminManager.Now.AddYears(1));
                break;
            default:
                break;
        }
    }

    public DateTime GetClock()
    {
        return AdminManager.Now;
    }

    public void InitializeDB()
    {
        AdminManager.ResetDB();
        AdminManager.InitializeDB();
    }

    public void ResedtDB()
    {
        AdminManager.ResetDB();
    }

    public Config GetConfig() => AdminManager.GetConfig();

    public void SetConfig(Config configuration) => AdminManager.SetConfig(configuration);
}
