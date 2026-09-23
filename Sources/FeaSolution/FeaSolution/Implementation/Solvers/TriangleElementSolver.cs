using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.Solvers;

/// <summary>
/// Линейный треугольный конечный элемент для задачи стационарной
/// теплопроводности: -div(λ ∇T) = 0.
/// </summary>
public sealed class TriangleElementSolver
{
    private CoordinateValue X1 { get; }
    private CoordinateValue Y1 { get; }
    private CoordinateValue X2 { get; }
    private CoordinateValue Y2 { get; }
    private CoordinateValue X3 { get; }
    private CoordinateValue Y3 { get; }

    // Геометрическая площадь
    public MatrixValue ElementSquare { get; }

    public LocalSymmetricMatrix<MatrixValue>? LocalMatrix { get; private set; }

    public TriangleElementSolver(IFiniteElement element)
    {
        FeaCommonException.ThrowIfTrue(element.ElementType.NodeType.Dimension != Dimensional.TwoDimensional,
            $"Accepted only TwoDimensional element. Current element dimention is '{element.ElementType.NodeType.Dimension.ToString()}'");

        FeaCommonException.ThrowIfTrue(element.Nodes.Count != 3,
            $"Accepted only Triangle element (node count is 3). Current node count is '{element.Nodes.Count}'");

        var nodes = element.Nodes.ToArray();

        X1 = nodes[0].X;
        X2 = nodes[1].X;
        X3 = nodes[2].X;

        Y1 = nodes[0].Y;
        Y2 = nodes[1].Y;
        Y3 = nodes[2].Y;

        ElementSquare = GetElementSquare();
    }

    /// <summary>
    /// Полный алгоритм получения локальной матрицы елемента.
    /// </summary>
    /// <param name="lambda">Коэффициент теплопроводности, W/(m·K)</param>
    /// <param name="thickness">Толщина, м</param>
    public LocalSymmetricMatrix<MatrixValue> BuildLocalMatrix(MatrixValue lambda, MatrixValue thickness)
    {
        if (LocalMatrix != null)
        {
            return LocalMatrix;
        }

        var b1 = Y2 - Y3;
        var b2 = Y3 - Y1;
        var b3 = Y1 - Y2;
        MatrixValue[] vectorB = [b1, b2, b3];

        var c1 = X3 - X2;
        var c2 = X1 - X3;
        var c3 = X2 - X1;
        MatrixValue[] vectorC = [c1, c2, c3];

        var multiplier= lambda * thickness / (4.0 * ElementSquare);

        LocalMatrix = new LocalSymmetricMatrix<MatrixValue>(3);
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
                LocalMatrix[i, j] = multiplier * (vectorB[i] * vectorB[j] + vectorC[i] * vectorC[j]);
            }
        }
    }

    public bool ValidateMatrixIsSymmetric(MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
            for (var j = i + 1; j < 3; j++)
                if (Math.Abs(LocalMatrix[i, j] - LocalMatrix[j, i]) > tolerance)
                    return false;
        return true;
    }

    public bool ValidateMatrixHasZeroRowSums(MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(LocalMatrix);

        for (var i = 0; i < 3; i++)
        {
            var sum = LocalMatrix[i, 0] + LocalMatrix[i, 1] + LocalMatrix[i, 2];
            if (Math.Abs(sum) > tolerance) return false;
        }
        return true;
    }

    private MatrixValue GetElementSquare()
    {
        var signedTwice =
            X1 * (Y2 - Y3) +
            X2 * (Y3 - Y1) +
            X3 * (Y1 - Y2);

        return 0.5 * Math.Abs(signedTwice);
    }
}