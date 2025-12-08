namespace BlImplementation;
using BlApi;
using BO;
using System.Collections.Generic;
using global::Helpers;
using DO;
using System.Linq;

internal class CourierImplementation : ICourier
{
    public void Add(int id, BO.Courier C)
    {
        throw new NotImplementedException();
    }

    public void Delete(int id, int courierId)
    {
        throw new NotImplementedException();
    }

    public BO.Courier Details(int id, int courierId)
    {
        throw new NotImplementedException();
    }

    public JobTypes EnterProgram(int id)
    {

        throw new NotImplementedException();
    }

    public IEnumerable<CourierInList> GetCouriers(int id, bool? includeInactive, CourierOptions? sort)
    {
        throw new NotImplementedException();
    }

    public void UpdateDetails(int id, BO.Courier C)
    {
        throw new NotImplementedException();
    }
}
