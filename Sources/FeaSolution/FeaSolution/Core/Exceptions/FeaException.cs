namespace FeaSolution.Core.Exceptions;

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