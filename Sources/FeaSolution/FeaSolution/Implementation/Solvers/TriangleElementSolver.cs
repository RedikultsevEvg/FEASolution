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

    // Физические параметры
    public MatrixValue Lambda { get; }   // коэффициент теплопроводности, W/(m·K)

    public MatrixValue Thickness { get; } // толщина, м

    // Геометрическая площадь
    public MatrixValue Area { get; }

    // Локальная матрица жёсткости 3x3
    public MatrixValue[,]? Ke { get; }

    public TriangleElementSolver(
        double x1, double y1,
        double x2, double y2,
        double x3, double y3,
        double lambda, double thickness)
    {
        X1 = x1; Y1 = y1;
        X2 = x2; Y2 = y2;
        X3 = x3; Y3 = y3;
        Lambda = lambda;
        Thickness = thickness;

        Area = ComputeArea();
        Ke = BuildLocalMatrix();
    }

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

        Area = ComputeArea();
    }

    /// <summary>
    /// Полный алгоритм получения локальной матрицы K_e.
    /// </summary>
    public LocalSymmetricMatrix<MatrixValue> BuildLocalMatrix(MatrixValue lambda, MatrixValue thickness)
    {
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

        var matrix = new LocalSymmetricMatrix<MatrixValue>(3);

        // убрать лишние
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                matrix[i, j] = coef * (b[i] * b[j] + c[i] * c[j]);
            }
        }
        return matrix;
    }
    
    /// <summary>
    /// Шаг 4: геометрическая площадь A = |A_signed|.
    /// </summary>
    private MatrixValue ComputeArea()
    {
        var signedTwice =
            X1 * (Y2 - Y3) +
            X2 * (Y3 - Y1) +
            X3 * (Y1 - Y2);

        return 0.5 * Math.Abs(signedTwice);
    }

    /// <summary>
    /// Полный алгоритм получения локальной матрицы K_e.
    /// </summary>
    private MatrixValue[,] BuildLocalMatrix()
    {
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
        var coef = Lambda * Thickness / (4.0 * Area);

        // Шаг 6: K[i,j] = coef * (b[i]*b[j] + c[i]*c[j])
        var k = new MatrixValue[3, 3];
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                k[i, j] = coef * (b[i] * b[j] + c[i] * c[j]);
            }
        }

        return k;
    }

    public bool ValidateIsSymmetric(MatrixValue tolerance = 1e-12)
    {
        for (var i = 0; i < 3; i++)
            for (var j = i + 1; j < 3; j++)
                if (Math.Abs(Ke[i, j] - Ke[j, i]) > tolerance)
                    return false;
        return true;
    }

    public bool ValidateHasZeroRowSums(MatrixValue tolerance = 1e-12)
    {
        for (var i = 0; i < 3; i++)
        {
            var sum = Ke[i, 0] + Ke[i, 1] + Ke[i, 2];
            if (Math.Abs(sum) > tolerance) return false;
        }
        return true;
    }
}