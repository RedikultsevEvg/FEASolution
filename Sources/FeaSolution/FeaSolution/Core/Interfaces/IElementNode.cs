using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// Finite element node. A node can be one-dimensional, two-dimensional, or three-dimensional.
/// </summary>
public interface IElementNode
{
    /// <summary>
    /// Finite element node type.
    /// </summary>
    ElementNodeType Type { get; }

    /// <summary>
    /// X coordinate.
    /// </summary>
    CoordinateValue X { get; set; }

    /// <summary>
    /// Y coordinate. Equals 0 if the node type is OneDimensional.
    /// </summary>
    CoordinateValue Y { get; set; }

    /// <summary>
    /// Z coordinate. Equals 0 if the node type is OneDimensional or TwoDimensional.
    /// </summary>
    CoordinateValue Z { get; set; }
}