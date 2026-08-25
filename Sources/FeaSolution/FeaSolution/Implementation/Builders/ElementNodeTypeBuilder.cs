using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.Builders;

public static class ElementNodeTypeBuilder
{
    public static ElementNodeType CreateNew1D() => new() { Dimension = Dimensional.OneDimensional };
    
    public static ElementNodeType CreateNew2D() => new() { Dimension = Dimensional.TwoDimensional };

    public static ElementNodeType CreateNew3D() => new() { Dimension = Dimensional.ThreeDimensional };
}