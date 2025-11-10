
namespace DO;

[Serializable]
public class DalAlreadyExistsException : Exception
{
    public DalAlreadyExistsException(string? message) : base(message) { }

}

[Serializable]
public class DalDoesNotExistException : Exception
{
    public DalDoesNotExistException(string? message) : base(message) { }
}

[Serializable]
public class  DalCannotBeNullException: Exception
{
    public DalCannotBeNullException(string? message) : base(message) { }
}