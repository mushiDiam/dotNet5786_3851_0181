using System;
using System.Collections;

namespace PL;
 
public class OrderTypesCollection : IEnumerable
{
    static readonly IEnumerable<BO.OrderTypes> s_enums =
        (Enum.GetValues(typeof(BO.OrderTypes)) as IEnumerable<BO.OrderTypes>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class EndTypesCollection : IEnumerable
{
    static readonly IEnumerable<BO.EndTypes> s_enums =
        (Enum.GetValues(typeof(BO.EndTypes)) as IEnumerable<BO.EndTypes>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class ScheduleStatusCollection : IEnumerable
{
    static readonly IEnumerable<BO.ScheduleStatus> s_enums =
        (Enum.GetValues(typeof(BO.ScheduleStatus)) as IEnumerable<BO.ScheduleStatus>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class OrderStatusCollection : IEnumerable
{
    static readonly IEnumerable<BO.OrderStatus> s_enums =
        (Enum.GetValues(typeof(BO.OrderStatus)) as IEnumerable<BO.OrderStatus>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class TransportationCollection : IEnumerable
{
    static readonly IEnumerable<BO.Transportation> s_enums =
        (Enum.GetValues(typeof(BO.Transportation)) as IEnumerable<BO.Transportation>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class JobTypesCollection : IEnumerable
{
    static readonly IEnumerable<BO.JobTypes> s_enums =
        (Enum.GetValues(typeof(BO.JobTypes)) as IEnumerable<BO.JobTypes>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class OrderInListOptionsCollection : IEnumerable
{
    static readonly IEnumerable<BO.OrderInListOptions> s_enums =
        (Enum.GetValues(typeof(BO.OrderInListOptions)) as IEnumerable<BO.OrderInListOptions>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class CourierInListOptionsCollection : IEnumerable
{
    static readonly IEnumerable<BO.CourierInListOptions> s_enums =
        (Enum.GetValues(typeof(BO.CourierInListOptions)) as IEnumerable<BO.CourierInListOptions>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

public class TimesCollection : IEnumerable
{
    static readonly IEnumerable<BO.Times> s_enums =
        (Enum.GetValues(typeof(BO.Times)) as IEnumerable<BO.Times>)!;
    public IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}


