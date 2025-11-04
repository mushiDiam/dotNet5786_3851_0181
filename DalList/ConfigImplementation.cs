namespace Dal;
using DalApi;


public class ConfigImplementation : IConfig
{
    public DateTime Clock
    {
        get => Config.Clock;
        set => Config.Clock = value;
    }
    public int MaxRange
    {
        get => Config.MaxRange;
        set => Config.MaxRange = value;
    }
    //...
    public void Reset()
    {
        Config.Reset();
    }
}
