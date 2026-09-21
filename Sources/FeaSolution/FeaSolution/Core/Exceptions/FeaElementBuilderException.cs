using FeaSolution.Core.Types;

namespace FeaSolution.Core.Exceptions;

/// <summary>
/// Generates Element builder exception.
/// </summary>
public class FeaElementBuilderException : FeaCommonException
{
    /// <summary>
    /// Generates Element builder exception.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    public FeaElementBuilderException(ElementNodeType nodeType) : base($"The current accepted node type is {nodeType.Dimension.ToString()}. You should use another method to specify all dimentions.")
    {
    }
    
    public FeaElementBuilderException(string message) : base(message)
    {
    }

    public static void ThrowIfTrue(bool condition, string message)
    {
        if (condition)
        {
            throw new FeaElementBuilderException(message);
        }
    }
}