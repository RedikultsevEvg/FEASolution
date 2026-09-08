namespace FeaSolution.Core.Exceptions;

/// <summary>
/// General FEA exception.
/// </summary>
public class FeaException : Exception
{
    public FeaException()
    {
    }

    public FeaException(string message)
        : base(message)
    {
    }

    public FeaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}