namespace FeaSolution.Implementation.Solvers;

/// <summary>
/// Линейный треугольный конечный элемент для задачи стационарной
/// теплопроводности: -div(λ ∇T) = 0.
/// </summary>
public sealed class TriangleElementSolver
{
    // Координаты узлов (локальные 1,2,3)
    public CoordinateValue X1 { get; }
    public CoordinateValue Y1 { get; }
    public CoordinateValue X2 { get; }
    public CoordinateValue Y2 { get; }
    public CoordinateValue X3 { get; }
    public CoordinateValue Y3 { get; }

    // Физические параметры
    public MatrixValue Lambda { get; }   // коэффициент теплопроводности, W/(m·K)

    public MatrixValue Thickness { get; } // толщина, м

    // Геометрическая площадь
    public MatrixValue Area { get; }

    // Локальная матрица жёсткости 3x3
    public MatrixValue[,] Ke { get; }

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

    /// <summary>
    /// Шаг 4: геометрическая площадь A = |A_signed|.
    /// </summary>
    private double ComputeArea()
    {
        double signedTwice =
            X1 * (Y2 - Y3) +
            X2 * (Y3 - Y1) +
            X3 * (Y1 - Y2);

        return 0.5 * Math.Abs(signedTwice);
    }

    /// <summary>
    /// Полный алгоритм получения локальной матрицы K_e.
    /// </summary>
    private double[,] BuildLocalMatrix()
    {
        // Шаг 2: коэффициенты b
        double b1 = Y2 - Y3;
        double b2 = Y3 - Y1;
        double b3 = Y1 - Y2;
        double[] b = [b1, b2, b3];

        // Шаг 3: коэффициенты c
        double c1 = X3 - X2;
        double c2 = X1 - X3;
        double c3 = X2 - X1;
        double[] c = [c1, c2, c3];

        // Шаг 5: множитель λ·t / (4A)
        double coef = Lambda * Thickness / (4.0 * Area);

        // Шаг 6: K[i,j] = coef * (b[i]*b[j] + c[i]*c[j])
        var k = new double[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                k[i, j] = coef * (b[i] * b[j] + c[i] * c[j]);
            }
        }

        return k;
    }

    public bool ValidateIsSymmetric(double tol = 1e-12)
    {
        for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
                if (Math.Abs(Ke[i, j] - Ke[j, i]) > tol)
                    return false;
        return true;
    }

    public bool ValidateHasZeroRowSums(double tol = 1e-12)
    {
        for (int i = 0; i < 3; i++)
        {
            double sum = Ke[i, 0] + Ke[i, 1] + Ke[i, 2];
            if (Math.Abs(sum) > tol) return false;
        }
        return true;
    }
}