namespace Dal;
using DalApi;
using DO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

internal class CourierImplementation : ICourier
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Courier item)
    {
        if (DataSource.Couriers.Exists(c => c.Id == item.Id))
            throw new DalAlreadyExistsException($"Courier with ID= {item.Id} already exists");

        ValidateCourier(item);

        DataSource.Couriers.Add(item);
    }

    private static void ValidateCourier(Courier item)
    {
        if (!IsValidId(item.Id))
            throw new DalInvalidValueException("ID must be exactly 9 digits");

        if (!IsValidPhoneNumber(item.Phone))
            throw new DalInvalidValueException("Phone number must start with '05' and contain exactly 10 digits");

        if (!IsValidFullName(item.Name))
            throw new DalInvalidValueException("Name must contain first and last name with English letters only");
    }

    private static bool IsValidId(int id)
    {
        // ID must be between 100000000 and 999999999 (exactly 9 digits)
        return id >= 100000000 && id <= 999999999;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return false;

        // Remove any spaces or dashes for validation
        string cleanedNumber = Regex.Replace(phoneNumber, @"[\s\-]", "");

        // Check if it starts with "05" and has exactly 10 digits
        return cleanedNumber.Length == 10 && cleanedNumber.StartsWith("05") && cleanedNumber.All(char.IsDigit);
    }

    private static bool IsValidFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        // Split by spaces and remove empty entries
        string[] nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Must have at least first and last name
        if (nameParts.Length < 2)
            return false;

        // Each part must contain only English letters
        return nameParts.All(part => part.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        if (DataSource.Couriers.Exists(c => c.Id == id))
            DataSource.Couriers.RemoveAll(c => c.Id == id);
        else
            throw new DalDoesNotExistException($"Courier with ID= {id} doesn't exists");
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(int id)
    {
        return DataSource.Couriers.FirstOrDefault(item => item.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(Func<Courier, bool> filter)
    {
        return DataSource.Couriers.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null) //stage 2
         => filter == null
             ? DataSource.Couriers.Select(item => item)
             : DataSource.Couriers.Where(filter);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(DO.Courier courier)
    {
        DO.Courier? existing = DataSource.Couriers.FirstOrDefault(c => c.Id == courier.Id);
       
        if (existing is null)
        {
            throw new DO.DalDoesNotExistException($"Courier with ID {courier.Id} does not exist");
        }

        ValidateCourier(courier);

        DataSource.Couriers.Remove(existing);
        DataSource.Couriers.Add(courier);
    }
}