using System.Diagnostics;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ElementNodes;
using Moq;

namespace FeaSolution.UnitTests.Performance;

/// <summary>
/// Тесты производительности <see cref="NodeMerger.MergeEqualNodes"/> на больших мешах.
/// </summary>
[TestFixture]
[Category("Performance")]
[TestOf(typeof(NodeMerger))]
public class NodeMergerPerformanceTests
{
    private const double Delta = 0.0;

    /// <summary>
    /// Прогрев JIT: первый вызов MergeEqualNodes компилирует методы,
    /// инициализирует статические поля и прогревает кэши — это может исказить замер.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        var elements = CreateGridMesh(50);
        NodeMerger.MergeEqualNodes(elements, Delta);
    }

    [Test]
    [TestCase(1_000, 50)]
    [TestCase(2_500, 50)]
    [TestCase(5_000, 100)]
    [TestCase(10_000, 200)]
    [TestCase(50_000, 1000)]
    public void MergeEqualNodes_WithManyElements_CompletesWithinReasonableTime(
        int elementCount, int expectedMaxMilliseconds)
    {
        // Arrange
        var elements = CreateGridMesh(elementCount);

        var totalOccurrences = elements.Sum(e => e.Nodes.Count);

        // Act
        var sw = Stopwatch.StartNew();
        NodeMerger.MergeEqualNodes(elements, Delta);
        sw.Stop();

        // Assert + диагностика
        var uniqueNodes = elements
            .SelectMany(e => e.Nodes)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Count();

        TestContext.WriteLine(
            $"Elements: {elementCount}, " +
            $"Nodes before merge: {totalOccurrences}, " +
            $"Nodes after merge: {uniqueNodes}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.That(
            sw.ElapsedMilliseconds,
            Is.LessThan(expectedMaxMilliseconds),
            $"MergeEqualNodes took too long: {sw.ElapsedMilliseconds} ms " +
            $"for {elementCount} elements ({totalOccurrences} occurrences).");
    }

    /// <summary>
    /// Основной регрессионный тест: 1000+ элементов должны мержиться без исключений
    /// и с корректным результатом (соседние треугольники делят узлы).
    /// </summary>
    [Test]
    public void MergeEqualNodes_WithMoreThanThousandElements_SucceedsAndSharesNodes()
    {
        const int elementCount = 1_500;

        var elements = CreateGridMesh(elementCount);
        Assert.That(elements.Count, Is.GreaterThan(1_000),
            "Test fixture must generate more than 1000 elements.");

        var sw = Stopwatch.StartNew();
        Assert.DoesNotThrow(() => NodeMerger.MergeEqualNodes(elements, Delta));
        sw.Stop();

        // Проверяем, что число уникальных узлов уменьшилось — соседние треугольники
        // действительно разделяют узлы после слияния.
        var uniqueAfterMerge = elements
            .SelectMany(e => e.Nodes)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Count();
        var totalOccurrences = elements.Sum(e => e.Nodes.Count);

        TestContext.WriteLine(
            $"Assembled {elementCount} elements in {sw.ElapsedMilliseconds} ms, " +
            $"occurrences: {totalOccurrences}, unique after merge: {uniqueAfterMerge}");

        Assert.That(uniqueAfterMerge, Is.LessThan(totalOccurrences),
            "После слияния должны появиться общие экземпляры узлов.");
    }

    /// <summary>
    /// Проверка, что рост времени близок к линейному/логарифмическому,
    /// а не квадратичному. На текущей реализации (двойной цикл) тест
    /// ожидаемо провалится — это и есть цель: зафиксировать требование
    /// к алгоритмической сложности.
    /// </summary>
    [Test]
    public void MergeEqualNodes_ScalesRoughlyLinearly_WithElementCount()
    {
        const int smallCount = 1_000;
        const int largeCount = 4_000;
        const int expectedScaling = 10; // допускаем до 10x при росте в 4 раза

        var smallTime = MeasureMerge(smallCount);
        var largeTime = MeasureMerge(largeCount);

        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        TestContext.WriteLine(
            $"small({smallCount})={smallTime} ms, " +
            $"large({largeCount})={largeTime} ms, " +
            $"ratio={ratio:F2} (elements ratio = {largeCount / (double)smallCount})");

        Assert.That(
            ratio,
            Is.LessThan(expectedScaling),
            $"MergeEqualNodes scaling is worse than expected: ratio = {ratio:F2}. " +
            "Current O(N^2) implementation likely needs spatial indexing (grid/hash/sort).");
    }

    private static long MeasureMerge(int elementCount)
    {
        var elements = CreateGridMesh(elementCount);

        // Прогрев на маленькой модели, чтобы GC/JIT не попали в замер.
        NodeMerger.MergeEqualNodes(CreateGridMesh(50), Delta);

        var sw = Stopwatch.StartNew();
        NodeMerger.MergeEqualNodes(elements, Delta);
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    // ---------------------------------------------------------------------
    // Генератор треугольной сетки
    // ---------------------------------------------------------------------

    /// <summary>
    /// Создаёт регулярную треугольную сетку (два треугольника на ячейку),
    /// в которой соседние треугольники имеют <b>разные экземпляры узлов</b>
    /// с одинаковыми координатами — именно такие узлы должен слить <see cref="NodeMerger"/>.
    /// </summary>
    private static IReadOnlyList<IFiniteElement> CreateGridMesh(int elementCount)
    {
        // Количество ячеек по стороне: cells^2 * 2 >= elementCount.
        var cellsPerSide = (int)Math.Ceiling(Math.Sqrt(elementCount / 2.0));
        var elements = new List<IFiniteElement>(elementCount);

        for (var i = 0; i < cellsPerSide && elements.Count < elementCount; i++)
        {
            for (var j = 0; j < cellsPerSide && elements.Count < elementCount; j++)
            {
                var x0 = i;
                var y0 = j;
                var x1 = i + 1;
                var y1 = j + 1;

                // Треугольник 1: (x0,y0), (x1,y0), (x1,y1)
                elements.Add(CreateTriangle(
                    (x0, y0, 0), (x1, y0, 0), (x1, y1, 0)));

                if (elements.Count >= elementCount) break;

                // Треугольник 2: (x0,y0), (x1,y1), (x0,y1)
                elements.Add(CreateTriangle(
                    (x0, y0, 0), (x1, y1, 0), (x0, y1, 0)));
            }
        }

        return elements;
    }

    private static IFiniteElement CreateTriangle(
        (double X, double Y, double Z) a,
        (double X, double Y, double Z) b,
        (double X, double Y, double Z) c)
    {
        var nodes = new List<IElementNode>
        {
            CreateNode(a.X, a.Y, a.Z),
            CreateNode(b.X, b.Y, b.Z),
            CreateNode(c.X, c.Y, c.Z),
        };

        var elementMock = new Mock<IFiniteElement>();
        elementMock.SetupGet(e => e.Nodes).Returns(nodes);
        return elementMock.Object;
    }

    private static IElementNode CreateNode(double x, double y, double z)
    {
        return new ElementNode(ElementNodeType.Type3D)
        {
            NodeId = string.Empty,
            X = x,
            Y = y,
            Z = z
        };
    }
}