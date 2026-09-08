using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.Builders;

public static class ElementNodeTypeBuilder
{
    /// <summary>
    /// Creates a new 1D node type.
    /// </summary>
    /// <returns>A new 1D node type.</returns>
    public static ElementNodeType CreateNew1D() => new() { Dimension = Dimensional.OneDimensional };

    /// <summary>
    /// Creates a new 2D node type.
    /// </summary>
    /// <returns>A new 2D node type.</returns>
    public static ElementNodeType CreateNew2D() => new() { Dimension = Dimensional.TwoDimensional };

    /// <summary>
    /// Creates a new 3D node type.
    /// </summary>
    /// <returns>A new 3D node type.</returns>
    public static ElementNodeType CreateNew3D() => new() { Dimension = Dimensional.ThreeDimensional };
}