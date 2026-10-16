using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.StiffnessMatrixs;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Stiffness matrix calculation logic for a linear triangular finite element
/// for the steady-state heat conduction problem: -div(λ ∇T) = 0.
/// </summary>
public sealed class StiffnessMatrixTriangle2DTemperatureLogic : IStiffnessMatrixLogic
{
    private IElementNode[] _nodes = [];

    private SmallStiffnessMatrix? LocalMatrix { get; set; }

    // todo: здесь надо учесть что в одно элементе могут быть дополнительные свободы (для других расчетов)
    /// <inheritdoc/> 
    public IStiffnessMatrix GetMatrix(IFiniteElement element)
    {
        FeaCommonException.ThrowIfTrue(element.Type.NodeType.Dimension != Dimensional.TwoDimensional,
            $"Accepted only TwoDimensional element. Current element dimention is '{element.Type.NodeType.Dimension.ToString()}'");

        FeaCommonException.ThrowIfTrue(element.Nodes.Count != 3,
            $"Accepted only Triangle element (node count is 3). Current node count is '{element.Nodes.Count}'");

        FeaCommonException.ThrowIfTrue(element.Type.Freedoms.Count != 1, 
            $"Accepted only element with one Freedom. Current freedom count is '{element.Type.Freedoms.Count}'");

        FeaCommonException.ThrowIfTrue(element.Type.Freedoms.ElementAt(0).Item1 != Freedom.Temperature, 
            $"Accepted one element with '{nameof(Freedom.Temperature)}'. Current element freedom is '{element.Type.Freedoms.ElementAt(0).Item1}'");

        if (element.Type.Freedoms.ElementAt(0).Item2 is not TemperatureElementOptions elementOption)
        {
            throw new FeaCommonException(
                $"Options must be of type '{nameof(TemperatureElementOptions)}'. " +
                $"Current options type is '{element.Type.Freedoms.ElementAt(0).Item2.GetType().Name}'");
        }

        var thermalConductivity = elementOption!.ThermalConductivity;
        var thickness = elementOption.Thickness;

        _nodes = [.. element.Nodes];

        var (x0, y0) = (_nodes[0].X, _nodes[0].Y);
        var (x1, y1) = (_nodes[1].X, _nodes[1].Y);
        var (x2, y2) = (_nodes[2].X, _nodes[2].Y);

        var square = GetElementSquare(x0, y0, x1, y1, x2, y2);
        FeaCommonException.ThrowIfTrue(square <= 0,
            "Element has zero or negative square (degenerate triangle).");

        MatrixValue[] vectorB = [y1 - y2, y2 - y0, y0 - y1];
        MatrixValue[] vectorC = [x2 - x1, x0 - x2, x1 - x0];

        var multiplier= thermalConductivity * thickness / (4.0 * square);

        LocalMatrix = new SmallStiffnessMatrix(_nodes);
        SetUpMatrix(multiplier, vectorB, vectorC);
        return LocalMatrix;
    }

    private void SetUpMatrix(double multiplier, MatrixValue[] vectorB, MatrixValue[] vectorC)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
        {
            for (var j = i; j < 3; j++)
            {
                var value = multiplier * (vectorB[i] * vectorB[j] + vectorC[i] * vectorC[j]);
                LocalMatrix.SetValue(i, j, value);
            }
        }
    }

    private static MatrixValue GetElementSquare(
        double x0, double y0, double x1, double y1, double x2, double y2)
    {
        var dx1 = x1 - x0; var dy1 = y1 - y0;
        var dx2 = x2 - x0; var dy2 = y2 - y0;

        return 0.5 * Math.Abs(dx1 * dy2 - dx2 * dy1);
    }
}