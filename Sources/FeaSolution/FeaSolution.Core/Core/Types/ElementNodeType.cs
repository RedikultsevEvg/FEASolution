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
    public static readonly ElementNodeType Type1D = new() { Dimension = Dimensional.OneDimensional };

    /// <summary>
    /// Gets a 2D node type.
    /// </summary>
    /// <returns>The 2D node type.</returns>
    public static readonly ElementNodeType Type2D = new() { Dimension = Dimensional.TwoDimensional };

    /// <summary>
    /// Gets a 3D node type.
    /// </summary>
    /// <returns>The 3D node type.</returns>
    public static readonly ElementNodeType Type3D = new() { Dimension = Dimensional.ThreeDimensional };

    /// <summary>
    /// Node dimensionality.
    /// </summary>
    public Dimensional Dimension { get; init; }
}