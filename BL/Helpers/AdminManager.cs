//using BO;
namespace Helpers;

using System.Runtime.CompilerServices;
using DalTest;
using DO;

/// <summary>
/// Internal BL manager for all Application's Configuration Variables and Clock logic policies
/// </summary>
internal static class AdminManager //stage 4
{
    #region Stage 4-7
    private static readonly DalApi.IDal s_dal = DalApi.Factory.Get; //stage 4

    /// <summary>
    /// Property for providing current application's clock value for any BL class that may need it
    /// </summary>
    internal static DateTime Now { get => s_dal.Config.Clock; } //stage 4

    internal static event Action? ConfigUpdatedObservers; //stage 5 - for config update observers
    internal static event Action? ClockUpdatedObservers; //stage 5 - for clock update observers


    /// <summary>
    /// Method to update application's clock from any BL class as may be required
    /// </summary>
    /// <param name="newClock">updated clock value</param>
    internal static void UpdateClock(DateTime newClock) //stage 4-7
    {
        var oldClock = s_dal.Config.Clock; //stage 4
        s_dal.Config.Clock = newClock; //stage 4

        //Add calls here to any logic method that should be called periodically,
        //after each clock update
        //for example, Periodic students' updates:
        // - Go through all students to update properties that are affected by the clock update
        // - (students become not active after 5 years etc.)

        //TO_DO: //stage 4
        //This is an example call, replace it with calls to properties needing the clock.
        //StudentManager.PeriodicStudentsUpdates(oldClock, newClock); //stage 4. to be removed in stage 7 and replaced as below
        //...

        //TO_DO: //stage 7
        _ =Task.Run(() => CourierManager.UpdateCourierActivity(oldClock, newClock));   
      

        //Calling all the observers of clock update
        ClockUpdatedObservers?.Invoke(); //prepared for stage 5
    }

    /// <summary>
    /// Method for providing current configuration variables values for any BL class that may need it
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
    internal static BO.Config GetConfig() //stage 4
    => new BO.Config()
    {
        
        //MaxRange = s_dal.Config.MaxRange,
        //TO_DO: //stage 4
        //add an assignment for each configuration property
        DeliveryWindow = s_dal.Config.MaxDeliveryTime,
        RiskRange = s_dal.Config.RiskRange,
        InactiveTime = s_dal.Config.InactiveTime,
        CompanyAddress = s_dal.Config.CompanyAddress ?? string.Empty,
        MaximumAirRange = s_dal.Config.MaxDeliveryDistance ?? 0.0,
        Latitude = s_dal.Config.CompanyLatitude ?? 0.0,
        Longitude = s_dal.Config.CompanyLongitude ?? 0.0,
        MaxiumDistance = s_dal.Config.MaxDeliveryDistance ?? 0.0, //Not sure if needed
        AverageCarSpeed = s_dal.Config.AverageCarSpeed,
        AverageMotorbikeSpeed = s_dal.Config.AverageMotorcycleSpeed,
        AverageBikeSpeed = s_dal.Config.AverageBikeSpeed,
        AverageWalkingSpeed = s_dal.Config.AverageWalkingSpeed,
        ManagerId = s_dal.Config.ManagerId,
        ManagerPassword = s_dal.Config.ManagerPassword,

    };

    /// <summary>
    /// Method for setting current configuration variables values for any BL class that may need it
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
    internal static void SetConfig(BO.Config configuration) //stage 4
    {
        bool configChanged = false; // stage 5

        if (s_dal.Config.MaxDeliveryTime != configuration.DeliveryWindow) //stage 4
        {
            s_dal.Config.MaxDeliveryTime = configuration.DeliveryWindow;
            configChanged = true;
        }
        if (s_dal.Config.RiskRange != configuration.RiskRange) //stage 4
        {
            s_dal.Config.RiskRange = configuration.RiskRange;
            configChanged = true;
        }
        if (s_dal.Config.InactiveTime != configuration.InactiveTime) //stage 4
        {
            s_dal.Config.InactiveTime = configuration.InactiveTime;
            configChanged = true;
        }
        if (s_dal.Config.CompanyAddress != configuration.CompanyAddress) //stage 4
        {
            s_dal.Config.CompanyAddress = configuration.CompanyAddress;
            configChanged = true;
        }
        if(s_dal.Config.MaxDeliveryDistance != configuration.MaximumAirRange) //stage 4
        {
            s_dal.Config.MaxDeliveryDistance = configuration.MaximumAirRange;
            configChanged = true;
        }
        if (s_dal.Config.CompanyLatitude != configuration.Latitude) //stage 4
        {
            s_dal.Config.CompanyLatitude = configuration.Latitude;
            configChanged = true;
        }
        if (s_dal.Config.CompanyLongitude != configuration.Longitude) //stage 4
        {
            s_dal.Config.CompanyLongitude = configuration.Longitude;
            configChanged = true;
        }
        if (s_dal.Config.MaxDeliveryDistance != configuration.MaxiumDistance) //stage 4
        {
            s_dal.Config.MaxDeliveryDistance = configuration.MaxiumDistance;
            configChanged = true;
        }
        if (s_dal.Config.AverageCarSpeed != configuration.AverageCarSpeed) //stage 4
        {
            s_dal.Config.AverageCarSpeed = configuration.AverageCarSpeed;
            configChanged = true;
        }
        if (s_dal.Config.AverageMotorcycleSpeed != configuration.AverageMotorbikeSpeed) //stage 4
        {
            s_dal.Config.AverageMotorcycleSpeed = configuration.AverageMotorbikeSpeed;
            configChanged = true;
        }
        if (s_dal.Config.AverageBikeSpeed != configuration.AverageBikeSpeed) //stage 4
        {
            s_dal.Config.AverageBikeSpeed = configuration.AverageBikeSpeed;
            configChanged = true;
        }
        if (s_dal.Config.AverageWalkingSpeed != configuration.AverageWalkingSpeed) //stage 4
        {
            s_dal.Config.AverageWalkingSpeed = configuration.AverageWalkingSpeed;
            configChanged = true;
        }
        if (s_dal.Config.ManagerId != configuration.ManagerId) //stage 4
        {
            s_dal.Config.ManagerId = configuration.ManagerId;
            configChanged = true;
        }
        if (s_dal.Config.ManagerPassword != configuration.ManagerPassword) //stage 4
        {
            s_dal.Config.ManagerPassword = configuration.ManagerPassword;
            configChanged = true;
        }
        //TO_DO: //stage 4
        //add a condition+assignment for each configuration property
        //...

        //Calling all the observers of configuration update
        if (configChanged) // stage 5
            ConfigUpdatedObservers?.Invoke(); // stage 5
    }

    internal static void ResetDB() //stage 4-7
    {
        lock (BlMutex) //stage 7
        {
            s_dal.resetDB(); //stage 4
            AdminManager.UpdateClock(AdminManager.Now); //stage 5 - needed since we want the label on Pl to be updated
            ConfigUpdatedObservers?.Invoke(); //stage 5 - needed to update PL
        }
    }

    internal static void InitializeDB() //stage 4-7
    {
        lock (BlMutex) //stage 7
        {
            DalTest.Initialization.Do(); //stage 4
            AdminManager.UpdateClock(AdminManager.Now);  //stage 5 - needed since we want the label on Pl to be updated           
           ConfigUpdatedObservers?.Invoke(); //stage 5 - needed to update PL
        }
    }

    internal static bool VerifyManager(int id)
    {
        return id == s_dal.Config.ManagerId;
    }
    #endregion Stage 4-7

    #region Stage 7 base

    /// <summary>    
    /// Mutex to use from BL methods to get mutual exclusion while the simulator is running
    /// </summary>
    internal static readonly object BlMutex = new(); // BlMutex = s_dal; // This field is actually the same as s_dal - it is defined for readability of locks
    /// <summary>
    /// The thread of the simulator
    /// </summary>
    private static volatile Thread? s_thread;
    /// <summary>
    /// The Interval for clock updating
    /// in minutes by second (default value is 1, will be set on Start())    
    /// </summary>
    private static int s_interval = 1;
    /// <summary>
    /// The flag that signs whether simulator is running
    /// 
    private static volatile bool s_stop = false;

    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7                                                 
    public static void ThrowOnSimulatorIsRunning()
    {
        if (s_thread is not null)
            throw new BlTemporaryNotAvailableException("Cannot perform the operation since Simulator is running");
    }

    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7                                                 
    internal static void Start(int interval)
    {
        if (s_thread is null)
        {
            s_interval = interval;
            s_stop = false;
            s_thread = new(clockRunner) { Name = "ClockRunner" };
            s_thread.Start();
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7                                                 
    internal static void Stop()
    {
        if (s_thread is not null)
        {
            s_stop = true;
            s_thread.Interrupt(); //awake a sleeping thread
            s_thread.Name = "ClockRunner stopped";
            s_thread = null;
        }
    }

    private static Task? _simulateTask = null;

    private static void clockRunner()
    {
        while (!s_stop)
        {
            UpdateClock(Now.AddMinutes(s_interval));

            //TO_DO: //stage 7
            //Add calls here to any logic simulation that was required in stage 7
            //for example: course registration simulation
            /*if (_simulateTask is null || _simulateTask.IsCompleted)//stage 7
                _simulateTask = Task.Run(() => StudentManager.SimulateCourseRegistrationAndGrade());
            */
            //etc...

            try
            {
                Thread.Sleep(1000); // 1 second
            }
            catch (ThreadInterruptedException) { }
        }
    }
    public static bool IsAdmin(int id) 
    {
        return id == s_dal.Config.ManagerId;
    }

    #endregion Stage 7 base
}