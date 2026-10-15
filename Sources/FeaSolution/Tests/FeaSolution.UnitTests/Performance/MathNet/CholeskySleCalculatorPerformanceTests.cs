using System.Diagnostics;
using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.StiffnessMatrixs;
using FeaSolution.MathNet.SleCalculation;
using MathNet.Numerics.LinearAlgebra.Double;
using Moq;

namespace FeaSolution.UnitTests.Performance.MathNet;

/// <summary>
/// Нагрузочные тесты <see cref="CholeskySleCalculator"/> на больших
/// плотных СЛАУ (до 1000 узлов).
///
/// Внимание: SolveSLE работает с плотной матрицей, поэтому сложность
/// по времени ~O(n³), по памяти ~O(n²). Это принципиально отличается
/// от нагрузочных тестов StiffnessMatrix (разреженная структура, O(n)).
/// </summary>
[TestFixture]
[Category("Performance")]
[TestOf(typeof(CholeskySleCalculator))]
public class CholeskySleCalculatorPerformanceTests
{
    private CholeskySleCalculator _calculator = null!;

    /// <summary>
    /// Прогрев JIT и инициализация статических полей до первого замера.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        _calculator = new CholeskySleCalculator();

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
        _calculator = new CholeskySleCalculator();
    }

    [Test]
    [TestCase(100, 1)]
    [TestCase(250, 5)]
    [TestCase(500, 20)]
    [TestCase(1_000, 100)]
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
        const int smallCount = 250;
        const int largeCount = 500;
        const double expectedCubicRatio = 12.0;
        const double tolerance = 1;

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
    [TestCase(500, 5.0)]
    [TestCase(1_000, 20.0)]
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
    [TestCase(2_000, 800)]
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
        var calculator = new CholeskySleCalculator();
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
}