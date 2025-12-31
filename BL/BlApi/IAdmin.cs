using BO;

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
}
