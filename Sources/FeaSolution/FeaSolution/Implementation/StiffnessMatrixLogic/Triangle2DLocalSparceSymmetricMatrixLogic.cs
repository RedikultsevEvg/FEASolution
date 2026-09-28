using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Линейный треугольный конечный элемент для задачи стационарной
/// теплопроводности: -div(λ ∇T) = 0.
/// </summary>
public sealed class Triangle2DLocalSparceSymmetricMatrixLogic : ILocalStiffnessMatrixLogic
{
    private IElementNode[] _nodes;

    private CoordinateValue X0 { get; set; }
    private CoordinateValue Y0 { get; set; }
    private CoordinateValue X1 { get; set; }
    private CoordinateValue Y1 { get; set; }
    private CoordinateValue X2 { get; set; }
    private CoordinateValue Y2 { get; set; }

    // Геометрическая площадь
    private MatrixValue ElementSquare { get; set; }

    private StiffnessMatrix? LocalMatrix { get; set; }

    /// <inheritdoc/> 
    public StiffnessMatrix GetLocalMatrix(IFiniteElement element, MatrixValue lambda, MatrixValue thickness)
    {
        FeaCommonException.ThrowIfTrue(element.Type.NodeType.Dimension != Dimensional.TwoDimensional,
            $"Accepted only TwoDimensional element. Current element dimention is '{element.Type.NodeType.Dimension.ToString()}'");

        FeaCommonException.ThrowIfTrue(element.Nodes.Count != 3,
            $"Accepted only Triangle element (node count is 3). Current node count is '{element.Nodes.Count}'");

        _nodes = element.Nodes.ToArray();

        X0 = _nodes[0].X;
        X1 = _nodes[1].X;
        X2 = _nodes[2].X;

        Y0 = _nodes[0].Y;
        Y1 = _nodes[1].Y;
        Y2 = _nodes[2].Y;

        ElementSquare = GetElementSquare();

        if (LocalMatrix != null)
        {
            return LocalMatrix;
        }

        var b1 = Y1 - Y2;
        var b2 = Y2 - Y0;
        var b3 = Y0 - Y1;
        MatrixValue[] vectorB = [b1, b2, b3];

        var c1 = X2 - X1;
        var c2 = X0 - X2;
        var c3 = X1 - X0;
        MatrixValue[] vectorC = [c1, c2, c3];

        var multiplier= lambda * thickness / (4.0 * ElementSquare);

        LocalMatrix = new StiffnessMatrix();
        SetUpMatrix(multiplier, vectorB, vectorC);
        return LocalMatrix;
    }

    private void SetUpMatrix(double multiplier, MatrixValue[] vectorB, MatrixValue[] vectorC)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
        {
            // start with new value due to symmetrix matrix.
            for (var j = i; j < 3; j++)
            {
                var nodeI = _nodes[i];
                var nodeJ = _nodes[j];
                LocalMatrix[nodeI, nodeJ] = multiplier * (vectorB[i] * vectorB[j] + vectorC[i] * vectorC[j]);
            }
        }
    }

    public bool ValidateMatrixIsSymmetric(MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
        for (var j = i + 1; j < 3; j++)
        {
            var nodeI = _nodes[i];
            var nodeJ = _nodes[j];

            if (Math.Abs(LocalMatrix[nodeI, nodeJ] - LocalMatrix[nodeJ, nodeI]) > tolerance)
                return false;
        }
        return true;
    }

    public bool ValidateMatrixHasZeroRowSums(MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
        {
            var nodeI = _nodes[i];

            var sum = LocalMatrix[nodeI, _nodes[0]] + LocalMatrix[nodeI, _nodes[1]] + LocalMatrix[nodeI, _nodes[2]];
            if (Math.Abs(sum) > tolerance) return false;
        }
        return true;
    }

    private MatrixValue GetElementSquare()
    {
        var signedTwice =
            X0 * (Y1 - Y2) +
            X1 * (Y2 - Y0) +
            X2 * (Y0 - Y1);

        return 0.5 * Math.Abs(signedTwice);
    }
}