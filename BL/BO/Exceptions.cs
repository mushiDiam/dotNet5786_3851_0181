namespace DO;

[Serializable]
public class BlAlreadyExistsException : Exception
{
    public BlAlreadyExistsException(string? message) : base(message) { }
    public BlAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class BlDoesNotExistException : Exception
{
    public BlDoesNotExistException(string? message) : base(message) { }
    public BlDoesNotExistException(string message, Exception inner) : base(message, inner) { }
}
[Serializable]
public class BlCannotBeNullException : Exception
{
    public BlCannotBeNullException(string? message) : base(message) { }
}
[Serializable]
public class BlInvalidOperationException : Exception
{
    public BlInvalidOperationException(string? message) : base(message) { }
    public BlInvalidOperationException(string message, Exception inner) : base(message, inner) { }
}
[Serializable]
public class BlOrderCanceledExeption : Exception
{
    public BlOrderCanceledExeption(string? message) : base(message) { }
}

[Serializable]
public class BlXmlFileLoadCreateException : Exception
{
    public BlXmlFileLoadCreateException(string? message) : base(message) { }
}


[Serializable]
public class  BlNullPropertyException : Exception
{
    public BlNullPropertyException(string? message) : base(message) { }
}
[Serializable]
public class BlUnauthorizedAccessException : Exception
{
    public BlUnauthorizedAccessException(string? message) : base(message) { }
}
[Serializable]
public class BlInvalidValueException : Exception
{
    public BlInvalidValueException(string? message) : base(message) { }
}
[Serializable]
public class BlEmptyListException : Exception
{
    public BlEmptyListException(string? message) : base(message) { }
}
[Serializable]
public class BlTemporaryNotAvailableException : Exception
{
    public BlTemporaryNotAvailableException(string? message) : base(message) { }
}
[Serializable]
public class BlFailedToConvert : Exception
{
    public BlFailedToConvert(string? message) : base(message) { }
    public BlFailedToConvert(string message, Exception inner) : base(message, inner) { }
}