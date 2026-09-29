using System.Diagnostics;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.UnitTests.Performance;

/// <summary>
/// Тесты производительности сборки глобальной матрицы жёсткости
/// на большом числе треугольных элементов (> 1000).
/// </summary>
[TestFixture]
[Category("Performance")]
public class SolutionBuilderPerformanceTests
{
    /// <summary>
    /// Прогрев JIT: первый вызов Assembly() компилирует методы и инициализирует
    /// статические поля — это может исказить замер.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        _ = new SolutionBuilder(TriangularMeshFactory.Create(100)).Assembly();
    }

    [Test]
    [TestCase(1_000)]
    [TestCase(2_500)]
    [TestCase(5_000)]
    [TestCase(10_000)]
    public void Assembly_WithManyElements_CompletesWithinReasonableTime(int elementCount)
    {
        // Arrange
        var model = TriangularMeshFactory.Create(elementCount);
        var builder = new SolutionBuilder(model);

        var nodeCount = model.Elements.Sum(e => e.Nodes.Count);
        var uniqueNodeCount = model.Elements
            .SelectMany(e => e.Nodes)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Count();

        // Act
        var sw = Stopwatch.StartNew();
        builder.Assembly();
        sw.Stop();

        // Assert + диагностика
        TestContext.WriteLine(
            $"Elements: {elementCount}, " +
            $"Node refs: {nodeCount}, " +
            $"Unique nodes: {uniqueNodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.That(
            sw.ElapsedMilliseconds,
            Is.LessThan(10_000),
            $"Assembly took too long: {sw.ElapsedMilliseconds} ms for {elementCount} elements");
    }

    /// <summary>
    /// Проверка, что рост времени сборки близок к линейному
    /// при увеличении числа элементов в 4 раза.
    /// </summary>
    [Test]
    public void Assembly_ScalesRoughlyLinearly_WithElementCount()
    {
        const int smallCount = 1_000;
        const int largeCount = 4_000;
        const int expectedScaling = 12;

        var smallTime = MeasureAssembly(smallCount);
        var largeTime = MeasureAssembly(largeCount);

        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        TestContext.WriteLine(
            $"small({smallCount})={smallTime} ms, " +
            $"large({largeCount})={largeTime} ms, " +
            $"ratio={ratio:F2} (elements ratio = {largeCount / (double)smallCount})");

        // Ожидаем ~4x, допускаем до 10x (запас на шум и GC).
        Assert.That(
            ratio,
            Is.LessThan(expectedScaling),
            $"Assembly scaling is worse than expected: ratio = {ratio:F2}");
    }

    /// <summary>
    /// Отдельный тест на 1000+ элементов — основной сценарий из задачи.
    /// </summary>
    [Test]
    public void Assembly_WithMoreThanThousandElements_Succeeds()
    {
        const int elementCount = 1_500;

        var model = TriangularMeshFactory.Create(elementCount);
        Assert.That(model.Elements.Count, Is.GreaterThan(1_000),
            "Test fixture must generate more than 1000 elements.");

        var builder = new SolutionBuilder(model);

        var sw = Stopwatch.StartNew();
        Assert.DoesNotThrow(() => builder.Assembly());
        sw.Stop();

        TestContext.WriteLine(
            $"Assembled {model.Elements.Count} elements in {sw.ElapsedMilliseconds} ms");
    }

    [Test]
    //[Explicit("Тяжёлый нагрузочный тест, запускать вручную.")]
    [TestCase(50_000)]
    [TestCase(100_000)]
    public void Assembly_VeryLargeMesh_Completes(int elementCount)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        var builder = new SolutionBuilder(model);

        var sw = Stopwatch.StartNew();
        builder.Assembly();
        sw.Stop();

        TestContext.WriteLine(
            $"elements={elementCount}, time={sw.ElapsedMilliseconds} ms");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(7_000));
    }

    private static long MeasureAssembly(int elementCount)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        var builder = new SolutionBuilder(model);

        // Прогрев на маленькой модели
        _ = new SolutionBuilder(TriangularMeshFactory.Create(50))
            .Assembly();

        var sw = Stopwatch.StartNew();
        builder.Assembly();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}