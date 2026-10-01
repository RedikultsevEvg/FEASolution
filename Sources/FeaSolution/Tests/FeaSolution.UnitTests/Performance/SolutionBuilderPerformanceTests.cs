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
    /// Прогрев JIT: первый вызов AssemblyAsync() компилирует методы и инициализирует
    /// статические поля — это может исказить замер.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        _ = new SolutionBuilder(TriangularMeshFactory.Create(100)).AssemblyAsync();
    }

    [Test]
    [TestCase(1_000, 10)]
    [TestCase(2_500, 50)]
    [TestCase(5_000, 100)]
    [TestCase(10_000, 500)]
    public async Task Assembly_WithManyElements_CompletesWithinReasonableTime(int elementCount, int expectedMaxMiliSeconds)
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
        await builder.AssemblyAsync();
        sw.Stop();

        // Assert + диагностика
        TestContext.WriteLine(
            $"Elements: {elementCount}, " +
            $"Node refs: {nodeCount}, " +
            $"Unique nodes: {uniqueNodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.That(
            sw.ElapsedMilliseconds,
            Is.LessThan(expectedMaxMiliSeconds),
            $"Assembly took too long: {sw.ElapsedMilliseconds} ms for {elementCount} elements");
    }

    /// <summary>
    /// Проверка, что рост времени сборки близок к линейному
    /// при увеличении числа элементов в 4 раза.
    /// </summary>
    [Test]
    public async Task Assembly_ScalesRoughlyLinearly_WithElementCount()
    {
        const int smallCount = 1_000;
        const int largeCount = 4_000;
        const int expectedScaling = 8;

        var smallTime = await MeasureAssemblyAsync(smallCount);
        var largeTime = await MeasureAssemblyAsync(largeCount);

        var ratio = largeTime / (double)Math.Max(smallTime, 1);

        TestContext.WriteLine(
            $"small({smallCount})={smallTime} ms, " +
            $"large({largeCount})={largeTime} ms, " +
            $"ratio={ratio:F2} (elements ratio = {largeCount / (double)smallCount})");

        // Ожидаем ~4x, допускаем до 8x (запас на шум и GC).
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
        Assert.DoesNotThrowAsync(() => builder.AssemblyAsync());
        sw.Stop();

        TestContext.WriteLine(
            $"Assembled {model.Elements.Count} elements in {sw.ElapsedMilliseconds} ms");
    }

    [Test]
    //[Explicit("Тяжёлый нагрузочный тест, запускать вручную.")]
    [TestCase(50_000, 2_000)]
    [TestCase(100_000, 4_000)]
    public async Task Assembly_VeryLargeMesh_Completes(int elementCount, int expectedMaxMiliSeconds)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        var builder = new SolutionBuilder(model);

        var nodeCount = model.Elements.Sum(e => e.Nodes.Count);
        var uniqueNodeCount = model.Elements
            .SelectMany(e => e.Nodes)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Count();

        var sw = Stopwatch.StartNew();
        await builder.AssemblyAsync();
        sw.Stop();

        TestContext.WriteLine(
            $"Elements: {elementCount}, " +
            $"Node refs: {nodeCount}, " +
            $"Unique nodes: {uniqueNodeCount}, " +
            $"Elapsed: {sw.ElapsedMilliseconds} ms");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(expectedMaxMiliSeconds));
    }

    private static async Task<long> MeasureAssemblyAsync(int elementCount)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        var builder = new SolutionBuilder(model);

        // Прогрев на маленькой модели
        _ = new SolutionBuilder(TriangularMeshFactory.Create(50))
            .AssemblyAsync();

        var sw = Stopwatch.StartNew();
        await builder.AssemblyAsync();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}