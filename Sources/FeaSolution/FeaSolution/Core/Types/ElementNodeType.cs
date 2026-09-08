using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

/// <summary>
/// Finite element node type.
/// </summary>
public class ElementNodeType
{
    /// <summary>
    /// Node dimensionality.
    /// </summary>
    public Dimensional Dimension { get; init; }
}