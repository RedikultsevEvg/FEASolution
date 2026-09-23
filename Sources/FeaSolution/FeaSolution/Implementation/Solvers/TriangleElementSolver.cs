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
    // Координаты узлов (локальные 1,2,3)
    private CoordinateValue X1 { get; }
    private CoordinateValue Y1 { get; }
    private CoordinateValue X2 { get; }
    private CoordinateValue Y2 { get; }
    private CoordinateValue X3 { get; }
    private CoordinateValue Y3 { get; }

    // Геометрическая площадь
    public MatrixValue Area { get; }

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

        Area = ComputeElementSquare();
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

        // Шаг 2: коэффициенты b
        var b1 = Y2 - Y3;
        var b2 = Y3 - Y1;
        var b3 = Y1 - Y2;
        MatrixValue[] b = [b1, b2, b3];

        // Шаг 3: коэффициенты c
        var c1 = X3 - X2;
        var c2 = X1 - X3;
        var c3 = X2 - X1;
        MatrixValue[] c = [c1, c2, c3];

        // Шаг 5: множитель λ·t / (4A)
        var coef = lambda * thickness / (4.0 * Area);

        // Шаг 6: K[i,j] = coef * (b[i]*b[j] + c[i]*c[j])

        LocalMatrix = new LocalSymmetricMatrix<MatrixValue>(3);

        // убрать лишние
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                LocalMatrix[i, j] = coef * (b[i] * b[j] + c[i] * c[j]);
            }
        }
        return LocalMatrix;
    }
    
    public bool ValidateMatrixIsSymmetric(MatrixValue tolerance = 1e-12)
    {
        for (var i = 0; i < 3; i++)
            for (var j = i + 1; j < 3; j++)
                if (Math.Abs(LocalMatrix[i, j] - LocalMatrix[j, i]) > tolerance)
                    return false;
        return true;
    }

    public bool ValidateMatrixHasZeroRowSums(MatrixValue tolerance = 1e-12)
    {
        for (var i = 0; i < 3; i++)
        {
            var sum = LocalMatrix[i, 0] + LocalMatrix[i, 1] + LocalMatrix[i, 2];
            if (Math.Abs(sum) > tolerance) return false;
        }
        return true;
    }

    private MatrixValue ComputeElementSquare()
    {
        var signedTwice =
            X1 * (Y2 - Y3) +
            X2 * (Y3 - Y1) +
            X3 * (Y1 - Y2);

        return 0.5 * Math.Abs(signedTwice);
    }
}