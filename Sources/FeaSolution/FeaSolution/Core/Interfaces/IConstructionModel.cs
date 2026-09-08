using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// Construction model.
/// </summary>
public interface IConstructionModel
{
    /// <summary>
    /// Allowed node type of the construction model.
    /// </summary>
    ElementNodeType AllowedNodeType { get; }

    /// <summary>
    /// Collection of finite elements of the construction model.
    /// </summary>
    ICollection<IFiniteElement> Elements { get; }
}