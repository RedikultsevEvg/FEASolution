using System.Diagnostics;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ElementNodes;

namespace FeaSolution.UnitTests.Performance;

/// <summary>
/// Нагрузочные тесты <see cref="FiniteElementBuilder"/>.
/// Ограничение: один элемент содержит не более 20 узлов (типичный максимум
/// для изопараметрических КЭ — квадратичные тетраэдры/гексаэдры).
/// Поэтому нагрузка создаётся большим числом независимых билдеров,
/// а не большим числом узлов в одном билдере.
/// </summary>
[TestFixture]
[Category("Performance")]
public class FiniteElementBuilderPerformanceTests
{
    /// <summary>
    /// Максимально допустимое число узлов в одном конечном элементе.
    /// </summary>
    private const int MaxNodesPerElement = 20;

    /// <summary>
    /// Прогрев JIT: первый Build/AddNode компилирует методы и инициализирует
    /// статические поля generic-коллекций.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        for (var i = 0; i < 100; i++)
        {
            BuildSingle1DElement(nodeCount: MaxNodesPerElement);
            BuildSingle2DElement(nodeCount: MaxNodesPerElement);
            BuildSingle3DElement(nodeCount: MaxNodesPerElement);
        }
    }

    #region Build — одиночный элемент

    [Test]
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(20)]
    public void Build1D_Max20Nodes_CompletesFast(int nodeCount)
    {
        // Arrange
        var builder = Create1DBuilder(nodeCount);

        // Act
        var sw = Stopwatch.StartNew();
        var element = builder.Build();
        sw.Stop();

        // Assert
        Assert.That(element.Nodes, Has.Count.EqualTo(nodeCount));
        TestContext.WriteLine($"1D build ({nodeCount} nodes): {sw.Elapsed.TotalMilliseconds:F3} ms");
        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(0.01),
            $"Build 1D ({nodeCount} nodes) took {sw.Elapsed.TotalMilliseconds:F3} ms");
    }

    [Test]
    [TestCase(3)]
    [TestCase(6)]
    [TestCase(15)]
    [TestCase(20)]
    public void Build2D_Max20Nodes_CompletesFast(int nodeCount)
    {
        // Arrange
        var builder = Create2DBuilder(nodeCount);

        // Act
        var sw = Stopwatch.StartNew();
        var element = builder.Build();
        sw.Stop();

        // Assert
        Assert.That(element.Nodes, Has.Count.EqualTo(nodeCount));
        TestContext.WriteLine($"2D build ({nodeCount} nodes): {sw.Elapsed.TotalMilliseconds:F3} ms");
        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(0.01),
            $"Build 2D ({nodeCount} nodes) took {sw.Elapsed.TotalMilliseconds:F3} ms");
    }

    [Test]
    [TestCase(4)]
    [TestCase(10)]
    [TestCase(20)]
    public void Build3D_Max20Nodes_CompletesFast(int nodeCount)
    {
        // Arrange
        var builder = Create3DBuilder(nodeCount);

        // Act
        var sw = Stopwatch.StartNew();
        var element = builder.Build();
        sw.Stop();

        // Assert
        Assert.That(element.Nodes, Has.Count.EqualTo(nodeCount));
        TestContext.WriteLine($"3D build ({nodeCount} nodes): {sw.Elapsed.TotalMilliseconds:F3} ms");
        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(0.01),
            $"Build 3D ({nodeCount} nodes) took {sw.Elapsed.TotalMilliseconds:F3} ms");
    }

    #endregion

    #region Many builds — throughput

    /// <summary>
    /// 10 000 сборок 2D-элементов по 20 узлов.
    /// Проверяем суммарное время и пропускную способность.
    /// </summary>
    [Test]
    [TestCase(1_000, 50)]
    [TestCase(10_000, 100)]
    public void Build_ManyElementsWith20Nodes_Throughput(int iterations, int maxMilliseconds)
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            var element = Create2DBuilder(MaxNodesPerElement).Build($"elem-{i}");
            Assert.That(element.Nodes, Has.Count.EqualTo(MaxNodesPerElement));
        }

        sw.Stop();

        var perElementUs = sw.Elapsed.TotalMilliseconds * 1000.0 / iterations;
        TestContext.WriteLine(
            $"{iterations} × build(20 nodes): {sw.ElapsedMilliseconds} ms " +
            $"(~{perElementUs:F2} µs/element)");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(maxMilliseconds),
            $"{iterations} builds took {sw.ElapsedMilliseconds} ms (limit {maxMilliseconds} ms)");
    }

    /// <summary>
    /// Изолированный замер одной операции Build при 20 узлах.
    /// </summary>
    [Test]
    public void Build_Single20NodeElement_UnderHundredMicroseconds()
    {
        const int iterations = 1_000;
        var builder = Create2DBuilder(MaxNodesPerElement);

        // Прогрев
        for (var i = 0; i < 100; i++) builder.Build();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) builder.Build();
        sw.Stop();

        var perBuildUs = sw.Elapsed.TotalMilliseconds * 1000.0 / iterations;
        TestContext.WriteLine($"Single build: ~{perBuildUs:F2} µs");

        Assert.That(perBuildUs, Is.LessThan(1),
            $"Single build took {perBuildUs:F2} µs (limit 100 µs)");
    }

    #endregion

    #region AddNode — throughput

    [Test]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void AddNode_1D_ManyNodes_Throughput(int totalNodes)
    {
        var builder = new FiniteElementBuilder();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < totalNodes; i++)
        {
            builder.AddNode(i * 0.001);
        }

        sw.Stop();

        TestContext.WriteLine($"AddNode 1D × {totalNodes}: {sw.ElapsedMilliseconds} ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(20),
            $"AddNode × {totalNodes} took {sw.ElapsedMilliseconds} ms");
    }

    [Test]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void AddNode_2D_ManyNodes_Throughput(int totalNodes)
    {
        var builder = new FiniteElementBuilder();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < totalNodes; i++)
        {
            builder.AddNode(i * 0.001, i * 0.002);
        }

        sw.Stop();

        TestContext.WriteLine($"AddNode 2D × {totalNodes}: {sw.ElapsedMilliseconds} ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(20),
            $"AddNode × {totalNodes} took {sw.ElapsedMilliseconds} ms");
    }

    [Test]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void AddNode_3D_ManyNodes_Throughput(int totalNodes)
    {
        var builder = new FiniteElementBuilder();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < totalNodes; i++)
        {
            builder.AddNode(i * 0.001, i * 0.002, i * 0.003);
        }

        sw.Stop();

        TestContext.WriteLine($"AddNode 3D × {totalNodes}: {sw.ElapsedMilliseconds} ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(20),
            $"AddNode × {totalNodes} took {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Сборка элемента через AddNode(IElementNode) — проверяем, что путь
    /// добавления готовых узлов не деградирует.
    /// </summary>
    [Test]
    public void AddNode_ByInstance_ManyNodes_Throughput()
    {
        const int totalNodes = 10_000;
        var builder = new FiniteElementBuilder();
        var source = new ElementNode(ElementNodeType.Type2D) { X = 1.0, Y = 2.0 };
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < totalNodes; i++)
        {
            builder.AddNode(source);
        }

        sw.Stop();

        TestContext.WriteLine($"AddNode(instance) × {totalNodes}: {sw.ElapsedMilliseconds} ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(10),
            $"AddNode(instance) × {totalNodes} took {sw.ElapsedMilliseconds} ms");
    }

    #endregion

    #region AddFreedom — throughput

    [Test]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void AddFreedom_ManyFreedoms_Throughput(int totalFreedoms)
    {
        var builder = new FiniteElementBuilder();
        var options = CreateOptions();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < totalFreedoms; i++)
        {
            builder.AddFreedom(Freedom.Temperature, options);
        }

        sw.Stop();

        TestContext.WriteLine($"AddFreedom × {totalFreedoms}: {sw.ElapsedMilliseconds} ms");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
            $"AddFreedom × {totalFreedoms} took {sw.ElapsedMilliseconds} ms");
    }

    #endregion

    #region Full pipeline

    /// <summary>
    /// Полный цикл билдера: 20 AddNode + N AddFreedom + Build, много раз.
    /// Отражает реальный профиль использования (построение одного КЭ).
    /// </summary>
    [Test]
    [TestCase(1_000, 2_000)]
    [TestCase(10_000, 15_000)]
    public void FullPipeline_BuildTwentyNodeElement_Repeated(int iterations, int maxMilliseconds)
    {
        const int freedoms = 3;
        var sw = Stopwatch.StartNew();

        for (var iter = 0; iter < iterations; iter++)
        {
            var b = new FiniteElementBuilder();

            for (var f = 0; f < freedoms; f++)
            {
                b.AddFreedom(Freedom.Temperature, CreateOptions());
            }

            for (var n = 0; n < MaxNodesPerElement; n++)
            {
                b.AddNode(n * 0.1, n * 0.2);
            }

            var element = b.Build($"e-{iter}");
            Assert.That(element.Nodes, Has.Count.EqualTo(MaxNodesPerElement));
        }

        sw.Stop();

        var perElementUs = sw.Elapsed.TotalMilliseconds * 1000.0 / iterations;
        TestContext.WriteLine(
            $"Full pipeline × {iterations}: {sw.ElapsedMilliseconds} ms (~{perElementUs:F2} µs/element)");

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(maxMilliseconds),
            $"Full pipeline × {iterations} took {sw.ElapsedMilliseconds} ms (limit {maxMilliseconds} ms)");
    }

    #endregion

    #region Scaling

    /// <summary>
    /// Проверяем, что время сборки масштабируется примерно линейно
    /// по числу узлов (до 20).
    /// </summary>
    [Test]
    public void Build_ScalesLinearly_WithNodeCount()
    {
        const int iterations = 5_000;

        var time4 = MeasureBuild(iterations, 4);
        var time20 = MeasureBuild(iterations, 20);

        var ratio = time20 / (double)Math.Max(time4, 1);
        var expected = 20 / 4.0;

        TestContext.WriteLine(
            $"build(4)={time4} ms, build(20)={time20} ms, " +
            $"ratio={ratio:F2} (nodes ratio={expected})");

        Assert.That(ratio, Is.LessThan(expected * 3),
            $"Build scaling worse than expected: ratio {ratio:F2}, expected ~{expected}");
    }

    #endregion

    #region Helpers

    private static long MeasureBuild(int iterations, int nodeCount)
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            Create2DBuilder(nodeCount).Build();
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static FiniteElementBuilder Create1DBuilder(int nodeCount)
    {
        Assert.That(nodeCount, Is.InRange(1, MaxNodesPerElement));
        var b = new FiniteElementBuilder();
        b.AddFreedom(Freedom.Temperature, CreateOptions());
        for (var i = 0; i < nodeCount; i++)
        {
            b.AddNode(i * 0.1);
        }

        return b;
    }

    private static FiniteElementBuilder Create2DBuilder(int nodeCount)
    {
        Assert.That(nodeCount, Is.InRange(1, MaxNodesPerElement));
        var b = new FiniteElementBuilder();
        b.AddFreedom(Freedom.Temperature, CreateOptions());
        for (var i = 0; i < nodeCount; i++)
        {
            b.AddNode(i * 0.1, i * 0.2);
        }

        return b;
    }

    private static FiniteElementBuilder Create3DBuilder(int nodeCount)
    {
        Assert.That(nodeCount, Is.InRange(1, MaxNodesPerElement));
        var b = new FiniteElementBuilder();
        b.AddFreedom(Freedom.Temperature, CreateOptions());
        for (var i = 0; i < nodeCount; i++)
        {
            b.AddNode(i * 0.1, i * 0.2, i * 0.3);
        }

        return b;
    }

    private static TemperatureElementOptions CreateOptions() => new()
    {
        ThermalConductivity = 2,
        Thickness = 1
    };

    private static IFiniteElement BuildSingle1DElement(int nodeCount) =>
        Create1DBuilder(nodeCount).Build();

    private static IFiniteElement BuildSingle2DElement(int nodeCount) =>
        Create2DBuilder(nodeCount).Build();

    private static IFiniteElement BuildSingle3DElement(int nodeCount) =>
        Create3DBuilder(nodeCount).Build();

    #endregion
}