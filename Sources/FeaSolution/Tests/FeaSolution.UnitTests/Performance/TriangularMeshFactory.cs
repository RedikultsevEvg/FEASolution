using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModels;
using FeaSolution.Implementation.ElementNodes;
using FeaSolution.Implementation.FiniteElements;

namespace FeaSolution.UnitTests.Performance;

/// <summary>
/// Фабрика для генерации регулярной треугольной сетки
/// с общими узлами (важно для реалистичной нагрузки на StiffnessMatrix).
/// </summary>
internal static class TriangularMeshFactory
{
    /// <summary>
    /// Создаёт конструкционную модель с количеством треугольных элементов
    /// не меньше <paramref name="targetElementCount"/>.
    /// </summary>
    public static IConstructionModel Create(int targetElementCount)
    {
        if (targetElementCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetElementCount));

        // Два треугольника на ячейку: 2 * nx * ny >= target
        var nx = (int)Math.Ceiling(Math.Sqrt(targetElementCount / 2.0));
        var ny = (int)Math.Ceiling(targetElementCount / (2.0 * nx));

        var nodes = new IElementNode[nx + 1, ny + 1];
        for (var i = 0; i <= nx; i++)
            for (var j = 0; j <= ny; j++)
            {
                nodes[i, j] = new ElementNode(ElementNodeType.Type2D)
                {
                    X = i,
                    Y = j
                };
            }

        var elements = new List<IFiniteElement>(2 * nx * ny);
        for (var i = 0; i < nx; i++)
            for (var j = 0; j < ny; j++)
            {
                var n00 = nodes[i, j];
                var n10 = nodes[i + 1, j];
                var n01 = nodes[i, j + 1];
                var n11 = nodes[i + 1, j + 1];

                elements.Add(CreateTriangle(n00, n10, n11));
                elements.Add(CreateTriangle(n00, n11, n01));
            }

        // todo: use builder
        return new ConstructionModel
        {
            AllowedNodeType = ElementNodeType.Type2D,
            Elements = elements,
            CommonFreedoms = [Freedom.Temperature]
        };
    }

    private static IFiniteElement CreateTriangle(
        IElementNode a, IElementNode b, IElementNode c)
    {
        var option = new TemperatureElementOptions
        {
            ThermalConductivity = 4,
            Thickness = 2
        };

        // todo: use builder
        return new FiniteElement
        {
            Nodes = [a, b, c],
            Type = new FiniteElementType
            {
                NodeType = ElementNodeType.Type2D,
                Freedoms = [ (Freedom.Temperature, option) ],
            }
        };
    }
}