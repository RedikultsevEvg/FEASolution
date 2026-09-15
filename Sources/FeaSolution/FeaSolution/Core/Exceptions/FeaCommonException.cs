namespace FeaSolution.Core.Exceptions;

/// <summary>
/// General FEA common exception.
/// </summary>
public class FeaCommonException : Exception
{
    public FeaCommonException()
    {
    }

    public FeaCommonException(string message)
        : base(message)
    {
    }

    public FeaCommonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}