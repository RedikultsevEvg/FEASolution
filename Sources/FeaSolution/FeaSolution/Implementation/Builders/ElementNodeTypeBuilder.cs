using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.Builders;

public static class ElementNodeTypeBuilder
{
    /// <summary>
    /// Создает новый тип узла 1D.
    /// </summary>
    /// <returns>Новый тип узла 1D.</returns>
    public static ElementNodeType CreateNew1D() => new() { Dimension = Dimensional.OneDimensional };

    /// <summary>
    /// Создает новый тип узла 2D.
    /// </summary>
    /// <returns>Новый тип узла 2D.</returns>
    public static ElementNodeType CreateNew2D() => new() { Dimension = Dimensional.TwoDimensional };

    /// <summary>
    /// Создает новый тип узла 3D.
    /// </summary>
    /// <returns>Новый тип узла 3D.</returns>
    public static ElementNodeType CreateNew3D() => new() { Dimension = Dimensional.ThreeDimensional };
}