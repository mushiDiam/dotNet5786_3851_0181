using BO;

namespace BlApi;

public interface IAdmin{
    void ResedtDB();
    void InitializeDB();
    DateTime GetClock();
    void ForwardClock(Times T);
    internal BO.Config GetConfig();
    internal void SetConfig(BO.Config C);
}
