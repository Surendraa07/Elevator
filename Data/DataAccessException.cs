namespace ElevatorSystem.Data;

/// <summary>
/// A database or file error, with a message that can be shown to the user as-is.
/// The original exception is kept in <see cref="Exception.InnerException"/> for diagnostics.
/// </summary>
public sealed class DataAccessException : Exception
{
    public DataAccessException(string userMessage, Exception? innerException = null)
        : base(userMessage, innerException)
    {
    }
}
