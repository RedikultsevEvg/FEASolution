using System.Diagnostics;
using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixs;
using Moq;

namespace FeaSolution.UnitTests.Performance;

/// <summary>
/// Нагрузочные тесты <see cref="StiffnessMatrix"/> на больших разреженных
/// симметричных матрицах (до 100 000 узлов).
/// </summary>
[TestFixture]
[Category("Performance")]
[Explicit]
[TestOf(typeof(StiffnessMatrix))]
public class StiffnessMatrixPerformanceTests
{
    /// <summary>
    /// Прогрев JIT и инициализация статических полей до первого замера.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        var matrix = new StiffnessMatrix();
        var nodes = Enumerable.Range(0, 100)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();

        for (var i = 0; i < nodes.Length; i++)
            matrix.AddValue(nodes[i], nodes[i], 1.0);

        _ = matrix.NonZeroMatrixValues().Count();
    }

    // ---------------------------------------------------------------------
    // AddValue: масштабирование по числу узлов
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(10_000, 400)]
    [TestCase(50_000, 2_000)]
    [TestCase(100_000, 7_000)]
    public void AddValue_WithManyDiagonalEntries_CompletesWithinReasonableTime(int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();

        // Act
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < nodeCount; i++)
            matrix.AddValue(nodes[i], nodes[i], 1.0);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Diagonal entries: {nodeCount}, " +
            $"NodeCount: {matrix.NodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(matrix.NodeCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Too slow: {sw.ElapsedMilliseconds} ms for {nodeCount} entries.");
        });
    }

    [Test]
    [TestCase(10_000, 1000)]
    [TestCase(50_000, 40_000)]
    public void AddValue_WithSparsePattern_CompletesWithinReasonableTime(int nodeCount, int expectedMaxMiliSeconds)
    {
        // Arrange — эмулируем FE-сетку: диагональ + ~4 соседа на узел.
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();
        const int neighborsPerNode = 4;
        var totalAdds = nodeCount * (neighborsPerNode + 1);

        // Act
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < nodeCount; i++)
        {
            matrix.AddValue(nodes[i], nodes[i], 2.0);

            for (var k = 1; k <= neighborsPerNode; k++)
            {
                var j = (i + k) % nodeCount;
                matrix.AddValue(nodes[i], nodes[j], -0.5);
            }
        }
        sw.Stop();

        var nonZeroCount = matrix.NonZeroMatrixValues().Count();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, " +
            $"Adds: {totalAdds}, " +
            $"Non-zero cells: {nonZeroCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.Multiple(() =>
        {
            Assert.That(matrix.NodeCount, Is.EqualTo(nodeCount));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
                $"Too slow: {sw.ElapsedMilliseconds} ms for {totalAdds} adds.");
        });
    }

    // ---------------------------------------------------------------------
    // AddValue: масштабирование при росте в 5 раз
    // ---------------------------------------------------------------------

    [Test]
    public void AddValue_ScalesRoughlyLinearly_WithNodeCount()
    {
        // Arrange
        const int smallCount = 10_000;
        const int largeCount = 50_000;
        const int expectedScaling = 25; // ожидаем ~5x, запас на GC/шум

        // Act
        var smallTime = MeasureSparseFilling(smallCount);
        var largeTime = MeasureSparseFilling(largeCount);
        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        // Assert
        TestContext.WriteLine(
            $"small({smallCount})={smallTime} ms, " +
            $"large({largeCount})={largeTime} ms, " +
            $"ratio={ratio:F2} (nodes ratio = {largeCount / (double)smallCount})");

        Assert.That(ratio, Is.LessThan(expectedScaling),
            $"Scaling worse than expected: ratio = {ratio:F2}");
    }

    // ---------------------------------------------------------------------
    // Чтение индексером
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(100_000)]
    public void Indexer_Get_OnLargeSparseMatrix_CompletesWithinReasonableTime(int nodeCount)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildSparseMatrix(nodes);
        const int readCount = 1_000_000;
        var random = new Random(42);

        // Act
        var sw = Stopwatch.StartNew();
        var sum = 0.0;
        for (var r = 0; r < readCount; r++)
        {
            var i = random.Next(nodeCount);
            var j = random.Next(nodeCount);
            sum += matrix[nodes[i], nodes[j]];
        }
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, Reads: {readCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Sum: {sum}");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5_000),
            $"Too slow: {sw.ElapsedMilliseconds} ms for {readCount} reads.");
    }

    // ---------------------------------------------------------------------
    // NonZeroMatrixValues
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(100_000)]
    public void NonZeroMatrixValues_FullTraversal_CompletesWithinReasonableTime(int nodeCount)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = BuildSparseMatrix(nodes);

        // Act
        var sw = Stopwatch.StartNew();
        var count = 0;
        var sum = 0.0;
        foreach (var (_, _, value) in matrix.NonZeroMatrixValues())
        {
            count++;
            sum += value;
        }
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, Non-zero cells: {count}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms, " +
            $"Sum: {sum}");

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5_000),
                $"Traversal too slow: {sw.ElapsedMilliseconds} ms.");
        });
    }

    // ---------------------------------------------------------------------
    // Симметрия на больших объёмах
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(100_000)]
    public void AddValue_SymmetricWrites_DoNotDuplicateCells(int nodeCount)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();
        const int neighborsPerNode = 4;

        // Act — пишем (i, j) и (j, i) для каждого ребра
        for (var i = 0; i < nodeCount; i++)
        {
            for (var k = 1; k <= neighborsPerNode; k++)
            {
                var j = (i + k) % nodeCount;
                matrix.AddValue(nodes[i], nodes[j], -0.5);
                matrix.AddValue(nodes[j], nodes[i], -0.5);
            }
        }

        var nonZeroCount = matrix.NonZeroMatrixValues().Count();

        // Assert — не должно быть дубликатов: ~nodeCount * neighborsPerNode / 2 ячеек
        var upperBound = nodeCount * neighborsPerNode;
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, Non-zero cells: {nonZeroCount}, " +
            $"Upper bound (with duplicates): {upperBound}");

        Assert.That(nonZeroCount, Is.LessThanOrEqualTo(upperBound),
            "Symmetric writes must collapse into one cell per pair.");
    }

    // ---------------------------------------------------------------------
    // Смешанная нагрузка: запись + чтение + перезапись
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(100_000)]
    public void MixedOperations_OnLargeMatrix_CompletesWithinReasonableTime(int nodeCount)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();
        var random = new Random(123);
        const int iterations = 500_000;

        // Act
        var sw = Stopwatch.StartNew();
        for (var it = 0; it < iterations; it++)
        {
            var i = random.Next(nodeCount);
            var j = random.Next(nodeCount);

            switch (it % 3)
            {
                case 0:
                    matrix.AddValue(nodes[i], nodes[j], 1.0);
                    break;
                case 1:
                    _ = matrix[nodes[i], nodes[j]];
                    break;
                case 2:
                    matrix[nodes[i], nodes[j]] = 0.5;
                    break;
            }
        }
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"Nodes: {nodeCount}, Iterations: {iterations}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(15_000),
            $"Mixed workload too slow: {sw.ElapsedMilliseconds} ms.");
    }

    // ---------------------------------------------------------------------
    // Память
    // ---------------------------------------------------------------------

    [Test]
    [TestCase(10_000)]
    public void SparseMatrix_WithManyEntries_UsesReasonableMemory(int nodeCount)
    {
        // Arrange
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        // Act
        BuildSparseMatrixInto(matrix, nodes);

        // Assert
        var after = GC.GetTotalMemory(forceFullCollection: true);

        var nonZeroCount = matrix.NonZeroMatrixValues().Count();
        var deltaBytes = after - before;
        var bytesPerCell = nonZeroCount > 0 ? deltaBytes / (double)nonZeroCount : 0;

        TestContext.WriteLine(
            $"Nodes: {nodeCount}, Non-zero cells: {nonZeroCount}, " +
            $"Delta: {deltaBytes / 1024.0 / 1024.0:F2} MB, " +
            $"Bytes/cell: {bytesPerCell:F1}");

        // Словарь + List + Dictionary<long,double> — реалистично ожидать
        // не более ~200 байт на ячейку (с большим запасом).
        Assert.That(bytesPerCell, Is.LessThan(60),
            $"Memory usage per cell is too high: {bytesPerCell:F1} bytes.");
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

    private static StiffnessMatrix BuildSparseMatrix(IElementNode[] nodes)
    {
        var matrix = new StiffnessMatrix();
        BuildSparseMatrixInto(matrix, nodes);
        return matrix;
    }

    private static void BuildSparseMatrixInto(StiffnessMatrix matrix, IElementNode[] nodes)
    {
        const int neighborsPerNode = 4;
        for (var i = 0; i < nodes.Length; i++)
        {
            matrix.AddValue(nodes[i], nodes[i], 2.0);

            for (var k = 1; k <= neighborsPerNode; k++)
            {
                var j = (i + k) % nodes.Length;
                matrix.AddValue(nodes[i], nodes[j], -0.5);
            }
        }
    }

    private static long MeasureSparseFilling(int nodeCount)
    {
        var nodes = CreateNodes(nodeCount);
        var matrix = new StiffnessMatrix();

        var sw = Stopwatch.StartNew();
        BuildSparseMatrixInto(matrix, nodes);
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}