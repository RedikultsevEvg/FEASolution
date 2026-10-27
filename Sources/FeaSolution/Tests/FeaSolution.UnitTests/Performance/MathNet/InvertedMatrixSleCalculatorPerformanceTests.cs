using System.Diagnostics;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.StiffnessMatrixs;
using FeaSolution.MathNet.SleCalculation;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;

namespace FeaSolution.UnitTests.Performance.MathNet;

/// <summary>
/// Нагрузочные тесты <see cref="InvertedMatrixSleCalculator"/> на больших
/// плотных СЛАУ (до 1000 узлов).
///
/// Внимание: SolveSLE работает с плотной матрицей, поэтому сложность
/// по времени ~O(n³), по памяти ~O(n²). Это принципиально отличается
/// от нагрузочных тестов StiffnessMatrix (разреженная структура, O(n)).
/// </summary>
[TestFixture]
[Category("Performance")]
[Explicit]
[TestOf(typeof(InvertedMatrixSleCalculator))]
public class InvertedMatrixSleCalculatorPerformanceTests
{
    private InvertedMatrixSleCalculator _calculator = null!;

    /// <summary>
    /// Прогрев JIT и инициализация статических полей до первого замера.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        _calculator = new InvertedMatrixSleCalculator();

        // Небольшой прогон, чтобы JIT скомпилировал горячие пути
        // (MathNet.Inverse, Vector.Build.Dense, создание SolutionItem).
        var nodes = CreateNodes(50);
        var matrix = BuildTridiagonal(nodes);
        var vectorF = BuildVectorF(nodes);

        _ = _calculator.SolveSLE(matrix, vectorF);
    }

    [SetUp]
    public void SetUp()
    {
        _calculator = new InvertedMatrixSleCalculator();
    }

    [Test]
    [TestCase(100, 5)]
    [TestCase(250, 20)]
    [TestCase(500, 200)]
    [TestCase(1_000, 1600)]
    public void SolveSLE_WithTridiagonalMatrix_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildTridiagonal(nodes);
        var expectedT = Enumerable.Repeat(1.0, nodeCount).ToArray();
        var vectorF = BuildVectorF(nodes, expectedT);

        // Act
        var sw = Stopwatch.StartNew();
        var result = _calculator.SolveSLE(matrix, vectorF);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Too slow: {sw.ElapsedMilliseconds} ms for {nodeCount} nodes.");
        });
    }

    [Test]
    public void SolveSLE_ScalesCubically_WithNodeCount()
    {
        // Arrange
        // Сравниваем T(2n) / T(n). Для O(n³) ожидаем ~8.
        // Даём запас 4× на шум и GC.
        const int smallCount = 250;
        const int largeCount = 500;
        const double expectedCubicRatio = 8.0;
        const double tolerance = 1.4;

        // Act
        var smallTime = MeasureSolveSle(smallCount);
        var largeTime = MeasureSolveSle(largeCount);
        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        // Assert
        TestContext.WriteLine(
            $"T({smallCount}) = {smallTime} ms, " +
            $"T({largeCount}) = {largeTime} ms, " +
            $"ratio = {ratio:F2}, " +
            $"expected ~{expectedCubicRatio} for O(n³)");

        Assert.That(ratio, Is.LessThan(expectedCubicRatio * tolerance),
            $"Scaling worse than O(n³): ratio = {ratio:F2}");
    }

    // ---------------------------------------------------------------------
    // Память: аллокации растут как O(n²)
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(500, 12.0)]
    [TestCase(1_000, 50.0)]
    public void SolveSLE_AllocationsAreQuadratic(int nodeCount, double expectedMb)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildTridiagonal(nodes);
        var vectorF = BuildVectorF(nodes, Enumerable.Repeat(1.0, nodeCount).ToArray());

        // Прогрев, чтобы исключить разовые аллокации из замера
        _calculator.SolveSLE(matrix, vectorF);

        // Act
        var before = GC.GetTotalAllocatedBytes(precise: true);
        _calculator.SolveSLE(matrix, vectorF);
        var after = GC.GetTotalAllocatedBytes(precise: true);

        var allocatedMb = (after - before) / 1024.0 / 1024.0;

        // Assert
        // Для n=1000 ожидаем: dense (8 МБ) + inverted (8 МБ) + result + буферы ≈ 16-32 МБ.

        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Allocated: {allocatedMb:F2} MB, " +
            $"Limit: {expectedMb:F2} MB");

        Assert.That(allocatedMb, Is.LessThan(expectedMb),
            $"Too much memory allocated: {allocatedMb:F2} MB for {nodeCount} nodes.");
    }

    [Test]
    [Explicit]
    [TestCase(2_000, 40_000)]
    public void SolveSLE_With2000Nodes_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange
        // 2000×2000 dense = 32 МБ; inverted = ещё 32 МБ. Итого ~64+ МБ.
        // Время: O(n³) — примерно 8× от n=1000.
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildTridiagonal(nodes);
        var vectorF = BuildVectorF(nodes, Enumerable.Repeat(1.0, nodeCount).ToArray());

        // Act
        var sw = Stopwatch.StartNew();
        var result = _calculator.SolveSLE(matrix, vectorF);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Too slow: {sw.ElapsedMilliseconds} ms for {nodeCount} nodes.");
        });
    }

    // ---------------------------------------------------------------------
    // InverseMathnetDenseMatrix: чистое обращение без сборки из IStiffnessMatrix
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(100, 5)]
    [TestCase(500, 200)]
    [TestCase(1_000, 1600)]
    public void InverseMathnetDenseMatrix_WithTridiagonalDenseMatrix_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange: собираем плотную матрицу ЗАРАНЕЕ, чтобы Stopwatch
        // измерял только само обращение.
        var dense = BuildTridiagonalDense(nodeCount);

        // Act
        var sw = Stopwatch.StartNew();
        var inverted = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(inverted.RowCount, Is.EqualTo(nodeCount));
            Assert.That(inverted.ColumnCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Inverse too slow: {sw.ElapsedMilliseconds} ms for {nodeCount}.");
        });
    }

    [Test]
    [TestCase(100, 5)]
    [TestCase(500, 200)]
    [TestCase(1_000, 1600)]
    public void InverseMathnetDenseMatrix_WithDiagonalDenseMatrix_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange: диагональная матрица — «тривиальный» случай для Inverse.
        // Это нижняя граница по времени: если и она медленная, дело не в алгоритме,
        // а в накладных расходах MathNet.
        var dense = DenseMatrix.Create(nodeCount, nodeCount, 0.0);
        for (var i = 0; i < nodeCount; i++)
            dense[i, i] = 2.0;

        // Act
        var sw = Stopwatch.StartNew();
        var inverted = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(inverted.RowCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Inverse too slow: {sw.ElapsedMilliseconds} ms for {nodeCount}.");
        });
    }

    [Test]
    [TestCase(100, 5)]
    [TestCase(500, 200)]
    [TestCase(1_000, 1600)]
    public void InverseMathnetDenseMatrix_WithFullDenseMatrix_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange: полностью заполненная матрица (худший случай по памяти,
        // но не по числу операций — они всё равно O(n³)).
        // Диагонально доминирующая, чтобы гарантировать невырожденность.
        var dense = DenseMatrix.Create(nodeCount, nodeCount, 0.0);
        var random = new Random(42);

        for (var i = 0; i < nodeCount; i++)
        {
            double rowSum = 0;
            for (var j = 0; j < nodeCount; j++)
            {
                if (i == j) continue;
                var v = random.NextDouble() * 0.1;
                dense[i, j] = v;
                rowSum += Math.Abs(v);
            }
            // Диагональ строго больше суммы остальных — гарантия SPD.
            dense[i, i] = rowSum + 1.0;
        }

        // Act
        var sw = Stopwatch.StartNew();
        var inverted = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(inverted.RowCount, Is.EqualTo(nodeCount));
            Assert.That(inverted.ColumnCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Inverse too slow: {sw.ElapsedMilliseconds} ms for {nodeCount}.");
        });
    }

    // ---------------------------------------------------------------------
    // InverseMathnetDenseMatrix: масштабирование O(n³)
    // ---------------------------------------------------------------------

    [Test]
    public void InverseMathnetDenseMatrix_ScalesCubically_WithSize()
    {
        // Сравниваем T(2n) / T(n). Для O(n³) ожидаем ~8.
        // Запас 1.4× на шум (как в SolveSLE_ScalesCubically_WithNodeCount).
        const int smallCount = 250;
        const int largeCount = 500;
        const double expectedCubicRatio = 8.0;
        const double tolerance = 1.4;

        // Arrange + Act
        var smallTime = MeasureInverseDense(smallCount);
        var largeTime = MeasureInverseDense(largeCount);
        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        // Assert
        TestContext.WriteLine(
            $"T({smallCount}) = {smallTime} ms, " +
            $"T({largeCount}) = {largeTime} ms, " +
            $"ratio = {ratio:F2}, " +
            $"expected ~{expectedCubicRatio} for O(n³)");

        Assert.That(ratio, Is.LessThan(expectedCubicRatio * tolerance),
            $"Scaling worse than O(n³): ratio = {ratio:F2}");
    }

    // ---------------------------------------------------------------------
    // InverseMathnetDenseMatrix: память O(n²)
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(500, 10.0)]
    [TestCase(1_000, 50.0)]
    public void InverseMathnetDenseMatrix_AllocationsAreQuadratic(
        int nodeCount, double expectedMb)
    {
        // Arrange
        var dense = BuildTridiagonalDense(nodeCount);

        // Прогрев, чтобы исключить разовые аллокации из замера
        _ = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);

        // Act
        var before = GC.GetTotalAllocatedBytes(precise: true);
        _ = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        var after = GC.GetTotalAllocatedBytes(precise: true);

        var allocatedMb = (after - before) / 1024.0 / 1024.0;

        // Assert
        // Для n=1000 ожидаем: inverted (8 МБ) + внутренние буферы LU ≈ 8-16 МБ.
        // (dense уже существует и в замер не входит.)
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Allocated: {allocatedMb:F2} MB, " +
            $"Limit: {expectedMb:F2} MB");

        Assert.That(allocatedMb, Is.LessThan(expectedMb),
            $"Too much memory allocated: {allocatedMb:F2} MB for {nodeCount} nodes.");
    }

    // ---------------------------------------------------------------------
    // InverseMathnetDenseMatrix: сравнение с прямым dense.Inverse()
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(500)]
    [TestCase(1_000)]
    public void InverseMathnetDenseMatrix_MatchesDirectMathNetInverse(int nodeCount)
    {
        // Arrange: тот же вход, что и у прямого dense.Inverse().
        // Тест документирует, что обёртка не добавляет накладных расходов
        // сверх MathNet.
        var dense = BuildTridiagonalDense(nodeCount);

        // Act
        var sw1 = Stopwatch.StartNew();
        var wrapped = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw1.Stop();

        var sw2 = Stopwatch.StartNew();
        var direct = dense.Inverse();
        sw2.Stop();

        // Assert — результаты должны совпадать
        Assert.Multiple(() =>
        {
            for (var i = 0; i < nodeCount; i++)
            {
                for (var j = 0; j < nodeCount; j++)
                {
                    Assert.That(wrapped[i, j], Is.EqualTo(direct[i, j]).Within(1e-9),
                        $"Расхождение в позиции ({i},{j})");
                }
            }
        });

        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Wrapped = {sw1.ElapsedMilliseconds} ms, " +
            $"Direct = {sw2.ElapsedMilliseconds} ms, " +
            $"Overhead = {(double)sw1.ElapsedMilliseconds / Math.Max(sw2.ElapsedMilliseconds, 1):F2}x");
    }

    [Test]
    [Explicit]
    [TestCase(2_000, 40_000)]
    public void InverseMathnetDenseMatrix_With2000Nodes_CompletesWithinReasonableTime(
        int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange
        // 2000×2000 dense = 32 МБ; inverted = ещё 32 МБ. Итого ~64+ МБ.
        // Время: O(n³) — примерно 8× от n=1000.
        // Плотная матрица собирается ЗАРАНЕЕ, чтобы Stopwatch измерял
        // только сам вызов InverseMathnetDenseMatrix.
        var dense = BuildTridiagonalDense(nodeCount);

        // Act
        var sw = Stopwatch.StartNew();
        var inverted = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Limit: {expectedMaxMiliSeconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(inverted.RowCount, Is.EqualTo(nodeCount));
            Assert.That(inverted.ColumnCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Inverse too slow: {sw.ElapsedMilliseconds} ms for {nodeCount}.");
        });
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static IElementNode[] CreateNodes(int count)
    {
        var nodes = new IElementNode[count];
        for (var i = 0; i < count; i++)
            nodes[i] = Mock.Of<IElementNode>();
        return nodes;
    }

    /// <summary>
    /// Трёхдиагональная SPD-матрица: K[i,i] = 2, K[i,i+1] = -1.
    /// Используется как реалистичный прототип матрицы жёсткости.
    /// </summary>
    private static IStiffnessMatrix BuildTridiagonal(IElementNode[] nodes)
    {
        var matrix = new StiffnessMatrix();

        for (var i = 0; i < nodes.Length; i++)
        {
            matrix.AddValue(nodes[i], nodes[i], 2.0);

            if (i < nodes.Length - 1)
                matrix.AddValue(nodes[i], nodes[i + 1], -1.0);
        }

        return matrix;
    }

    private static SolutionItem[] BuildVectorF(
        IElementNode[] nodes,
        double[]? values = null)
    {
        var size = nodes.Length;
        var f = new SolutionItem[size];

        for (var i = 0; i < size; i++)
        {
            f[i] = new SolutionItem
            {
                Node = nodes[i],
                Freedom = Freedom.Temperature,
                Value = values?[i] ?? 1.0
            };
        }

        return f;
    }

    private static long MeasureSolveSle(int nodeCount)
    {
        var calculator = new InvertedMatrixSleCalculator();
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildTridiagonal(nodes);
        var vectorF = BuildVectorF(nodes, Enumerable.Repeat(1.0, nodeCount).ToArray());

        // Прогрев
        calculator.SolveSLE(matrix, vectorF);

        var sw = Stopwatch.StartNew();
        calculator.SolveSLE(matrix, vectorF);
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// Извлекает разреженные значения из IStiffnessMatrix для сравнения.
    /// </summary>
    private static List<(int i, int j, double v)> ExtractValues(IStiffnessMatrix matrix)
    {
        var result = new List<(int, int, double)>();
        foreach (var (i, j, v) in matrix.NonZeroMatrixValuesWithIndexes())
            result.Add((i, j, v));
        return result;
    }

    /// <summary>
    /// Собирает плотную трёхдиагональную SPD-матрицу:
    ///   K[i,i]   =  2
    ///   K[i,i+1] = -1  (и симметрично)
    /// </summary>
    private static DenseMatrix BuildTridiagonalDense(int size)
    {
        var dense = DenseMatrix.Create(size, size, 0.0);

        for (var i = 0; i < size; i++)
        {
            dense[i, i] = 2.0;

            if (i < size - 1)
            {
                dense[i, i + 1] = -1.0;
                dense[i + 1, i] = -1.0;
            }
        }

        return dense;
    }

    /// <summary>
    /// Замеряет только вызов InverseMathnetDenseMatrix, без сборки плотной матрицы.
    /// </summary>
    private static long MeasureInverseDense(int nodeCount)
    {
        var dense = BuildTridiagonalDense(nodeCount);

        // Прогрев
        _ = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);

        var sw = Stopwatch.StartNew();
        _ = InvertedMatrixSleCalculator.InverseMathnetDenseMatrix(dense);
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}