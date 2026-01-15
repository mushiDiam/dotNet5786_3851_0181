using BO;
using System.Threading.Tasks;

namespace BlApi;

public interface IAdmin{
    void ResetDB();
    void InitializeDB();
    DateTime GetClock();
    void ForwardClock(Times T);
    public BO.Config GetConfig();
    public void SetConfig(BO.Config C);

    #region Stage 5
    void AddConfigObserver(Action configObserver);
    void RemoveConfigObserver(Action configObserver);
    void AddClockObserver(Action clockObserver);
    void RemoveClockObserver(Action clockObserver);
    #endregion Stage 5

    // Geocoding helper exposed by BL so PL can request coordinates without doing HTTP/json itself.
    Task<(double? Lat, double? Lon)> GetCoordinatesFromAddressAsync(string address);

    #region Stage 7

    void StartSimulator(int interval);
    void StopSimulator();

    #endregion Stage 7
}
