using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

/// <summary>
/// Finite element node type.
/// </summary>
public class ElementNodeType
{
    /// <summary>
    /// Gets a 1D node type.
    /// </summary>
    /// <returns>The 1D node type.</returns>
    public static ElementNodeType Type1d = new ElementNodeType { Dimension = Dimensional.OneDimensional };

    /// <summary>
    /// Gets a 2D node type.
    /// </summary>
    /// <returns>The 2D node type.</returns>
    public static ElementNodeType Type2d = new ElementNodeType { Dimension = Dimensional.TwoDimensional };

    /// <summary>
    /// Gets a 3D node type.
    /// </summary>
    /// <returns>The 3D node type.</returns>
    public static ElementNodeType Type3d = new ElementNodeType { Dimension = Dimensional.ThreeDimensional };

    /// <summary>
    /// Node dimensionality.
    /// </summary>
    public Dimensional Dimension { get; init; }
}