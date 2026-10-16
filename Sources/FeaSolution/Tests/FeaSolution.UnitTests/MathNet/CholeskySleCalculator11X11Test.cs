using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.MathNet.SleCalculation;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

/// <summary>
/// Тесты для системы размерности 11×11 — нечётный размер,
/// трёхдиагональная SPD-матрица (дискретный оператор Лапласа),
/// а также диагональные и вырожденные случаи.
/// </summary>
[TestFixture]
[TestOf(typeof(CholeskySleCalculator))]
public class CholeskySleCalculator11X11Test : SleCalculatorTestBase
{
    private const int MatrixSize = 11;

    private CholeskySleCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _calculator = new CholeskySleCalculator();
    }

    // ===============================================================
    // Тест 1. Трёхдиагональная матрица + известное решение T = [1..11]
    // ===============================================================

    [Test]
    public void SolveSLE_WhenTridiagonalMatrix_ReturnsKnownSolution()
    {
        // Arrange
        // Задаём желаемое решение T = [1, 2, 3, ..., 11] и вычисляем F = K·T
        // программно, чтобы избежать ошибок ручного счёта.
        var nodes = MakeNodes(MatrixSize);
        var values = MakeTridiagonalMatrixValues(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes, values);

        var expectedT = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        var vectorFValues = MultiplyMatrixVector(values, expectedT, MatrixSize);
        var vectorF = MakeVector(nodes, vectorFValues);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(MatrixSize));

        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    // ===============================================================
    // Тест 2. Трёхдиагональная матрица + T = квадраты
    // ===============================================================

    [Test]
    public void SolveSLE_WhenTridiagonalMatrix_ReturnsQuadraticSolution()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var values = MakeTridiagonalMatrixValues(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes, values);

        // Желаемое решение: T_i = i²
        var expectedT = new double[MatrixSize];
        for (int i = 0; i < MatrixSize; i++)
            expectedT[i] = (i + 1) * (i + 1);

        var f = MultiplyMatrixVector(values, expectedT, MatrixSize);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    // ===============================================================
    // Тест 3. Диагональная матрица → поэлементное деление
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11DiagonalMatrix_ReturnsElementwiseDivision()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes,
            DiagonalMatrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));

        var vectorF = MakeVector(nodes, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Value, Is.EqualTo(1.0).Within(Tolerance),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    // ===============================================================
    // Тест 4. Единичная матрица 11×11 → T = F
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11IdentityMatrix_ReturnsRightHandSide()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes,
            DiagonalMatrix(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

        var expected = new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110 };
        var vectorF = MakeVector(nodes, expected);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Value, Is.EqualTo(expected[i]),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    // ===============================================================
    // Тест 5. Симметричная матрица с Dirichlet-паттерном
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11MatrixWithDirichletPattern_ReturnsKnownSolution()
    {
        // Arrange
        // Матрица после учёта условий Дирихле:
        //   - узел 1 зафиксирован (K[0,0] = 1, внедиагональные = 0),
        //   - узел 11 зафиксирован (K[10,10] = 1, внедиагональные = 0),
        //   - узлы 2..10 связаны трёхдиагональной матрицей.
        //
        // Задаём желаемое решение T и вычисляем F = K·T программно.
        var nodes = MakeNodes(MatrixSize);

        var values = new List<(int, int, double)>
        {
            (0, 0, 1.0),
            (10, 10, 1.0)
        };
        for (int i = 1; i <= 9; i++)
            values.Add((i, i, 2.0));
        for (int i = 1; i <= 8; i++)
            values.Add((i, i + 1, -1.0));

        var matrixKD = MakeMatrixMock(MatrixSize, nodes, values);

        // Желаемое решение: T = [20, 32.5, 44, 54.5, 64, 72.5, 80, 86.5, 92, 96.5, 100]
        var expectedT = new double[]
        {
            20.0, 32.5, 44.0, 54.5, 64.0, 72.5, 80.0, 86.5, 92.0, 96.5, 100.0
        };

        var f = MultiplyMatrixVector(values, expectedT, MatrixSize);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixKD, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    // ===============================================================
    // Тест 6. Ссылки на узлы и Freedom
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11Matrix_ResultItemsReferenceCorrectNodesAndFreedom()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes,
            DiagonalMatrix(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

        var vectorF = MakeVector(nodes, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
            {
                Assert.That(result[i].Node, Is.SameAs(nodes[i]),
                    $"Узел {i + 1} не совпадает по ссылке");
                Assert.That(result[i].Freedom, Is.EqualTo(Freedom.Temperature),
                    $"Freedom узла {i + 1} отличается");
            }
        });
    }

    // ===============================================================
    // Тест 7. Сингулярная матрица → исключение
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11SingularMatrix_ThrowsFeaCommonException()
    {
        // Arrange: диагональная матрица с нулём на одной из позиций — вырождена.
        var nodes = MakeNodes(MatrixSize);
        var diag = Enumerable.Repeat(1.0, MatrixSize).ToArray();
        diag[5] = 0.0;   // нулевой диагональный элемент → сингулярность

        var singularMatrixK = MakeMatrixMock(MatrixSize, nodes, DiagonalMatrix(diag));

        var vectorF = MakeVector(nodes,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(singularMatrixK, vectorF));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("Can't solve the SLE: the matrix is not symmetric positive definite or is ill-conditioned."));
    }

    // ===============================================================
    // Тест 8. Нулевая RHS → нулевое решение
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11MatrixWithZeroRhs_ReturnsZeroVector()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes, MakeTridiagonalMatrixValues(MatrixSize));
        var vectorF = MakeVector(nodes, new double[MatrixSize]);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result.All(r => r.Value == 0.0), Is.True,
            "Все элементы решения должны быть равны нулю");
    }

    // ===============================================================
    // Тест 9. Однократное чтение разреженных данных
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11Matrix_ReadsNonZeroValuesFromMatrixOnce()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes, MakeTridiagonalMatrixValues(MatrixSize));
        var vectorF = MakeVector(nodes,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        // Act
        _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        matrixK.Received(1).NonZeroMatrixValuesWithIndexes();
        _ = matrixK.Received(1).Nodes;
    }

    // ===============================================================
    // Тест 10. Порядок узлов совпадает с matrix.Nodes
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11Matrix_ResultOrderMatchesNodesOrder()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes,
            DiagonalMatrix(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        var vectorF = MakeVector(nodes,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(result[i].Node, Is.SameAs(nodes[i]),
                    $"Порядок узлов нарушен в позиции {i}");
        });
    }

    // ===============================================================
    // Тест 11. Косвенная проверка: K · T = F
    // ===============================================================

    [Test]
    public void SolveSLE_When11x11Matrix_ResultSatisfiesOriginalEquation()
    {
        // Arrange
        var nodes = MakeNodes(MatrixSize);
        var values = MakeTridiagonalMatrixValues(MatrixSize);
        var matrixK = MakeMatrixMock(MatrixSize, nodes, values);

        var vectorF = MakeVector(nodes,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert: собираем K симметрично и умножаем на T.
        var computedF = MultiplyMatrixVector(values,
            result.Select(r => r.Value).ToArray(), MatrixSize);

        Assert.Multiple(() =>
        {
            for (int i = 0; i < MatrixSize; i++)
                Assert.That(computedF[i], Is.EqualTo(vectorF[i].Value).Within(Tolerance),
                    $"K·T не совпадает с F в позиции {i + 1}: " +
                    $"получено {computedF[i]}, ожидалось {vectorF[i].Value}");
        });
    }
}