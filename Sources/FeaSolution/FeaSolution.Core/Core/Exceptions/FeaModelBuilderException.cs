namespace FeaSolution.Core.Exceptions;

/// <summary>
/// Generates Model builder exception.
/// </summary>
public class FeaModelBuilderException(string message) : FeaCommonException(message)
{
    public new static void ThrowIfTrue(bool condition, string message)
    {
        if (condition)
        {
            throw new FeaModelBuilderException(message);
        }
    }
}