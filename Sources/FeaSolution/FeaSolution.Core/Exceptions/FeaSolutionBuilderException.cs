namespace FeaSolution.Core.Exceptions;

/// <summary>
/// Generates solution builder exception.
/// </summary>
public class FeaSolutionBuilderException(string message) : FeaCommonException(message)
{
    public new static void ThrowIfTrue(bool condition, string message)
    {
        if (condition)
        {
            throw new FeaSolutionBuilderException(message);
        }
    }
}