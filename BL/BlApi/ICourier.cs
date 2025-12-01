using BO;

namespace BlApi;

public interface ICourier{

    JobTypes EnterProgram(int id);
    IEnumerable<BO.CourierInList> GetCouriers(int id, bool? includeInactive, CourierOptions? sort);
    BO.Courier Details(int id , int courierId);
    void UpdateDetails(int id, BO.Courier C);
    void Delete(int id , int courierId);
    void Add(int id , BO.Courier C);
}
