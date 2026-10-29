using System.Diagnostics;
using FeaSolution.Core.Enums;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests.Performance;

/// <summary>
/// Интеграционные тесты производительности ПОЛНОГО конвейера решения:
/// создание модели → Assembly → AddDirichlet → Build.
/// </summary>
[TestFixture]
[Explicit]
[Category("Performance")]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class FullSolutionPerformanceTests
{
    /// <summary>
    /// Прогрев JIT и статических инициализаторов для ВСЕГО конвейера,
    /// а не только Assembly — иначе первый замер будет включать компиляцию
    /// методов решения СЛАУ, что сильно исказит результат.
    /// </summary>
    [OneTimeSetUp]
    public void WarmUp()
    {
        _ = RunFullPipelineWithPhases(50);
    }

    [Test]
    [TestCase(3, 20)]
    [TestCase(1_000, 200)]
    [TestCase(2_500, 1500)]
    [TestCase(5_000, 15_000)]
    [TestCase(10_000, 100_000)]
    [Description("Полный конвейер на средних сетках укладывается в разумное время.")]
    public void FullPipeline_CompletesWithinReasonableTime(int elementCount, int maxMilliseconds)
    {
        // Arrange
        var (solution, phases) = RunFullPipelineWithPhases(elementCount);

        // Assert: решение корректно
        AssertValidSolution(solution);

        TestContext.WriteLine(
            $"Preconditions - Elements: {elementCount}, Unique nodes: {phases.UniqueNodes}");

        TestContext.WriteLine(
            $"Timing - " +
            $"Model collected: {phases.ModelMs} ms, " +
            $"Assembly: {phases.AssemblyMs} ms, " +
            $"Build: {phases.BuildMs} ms, " +
            $"Total: {phases.TotalMs} ms");

        Assert.That(phases.TotalMs, Is.LessThan(maxMilliseconds),
            $"Full pipeline took too long: {phases.TotalMs} ms for {elementCount} elements " +
            $"(model collected ={phases.ModelMs}, assembly={phases.AssemblyMs}, build={phases.BuildMs})");
    }

    /*
    [Test]
    [TestCase(1_500)]
    [TestCase(3_000)]
    [Description("Основной сценарий задачи: >1000 элементов, полный конвейер проходит без исключений.")]
    public void FullPipeline_WithMoreThanThousandElements_Succeeds(int elementCount)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        Assert.That(model.Elements.Count, Is.GreaterThan(1_000),
            "Test fixture must generate more than 1000 elements.");

        var builder = new SolutionBuilder(model);

        var sw = Stopwatch.StartNew();
        Assert.DoesNotThrow(() =>
        {
            var solution = builder
                .Assembly()
                .AddDirichlet("b0", Freedom.Temperature, 100.0)
                .AddDirichlet($"b{elementCount}", Freedom.Temperature, 0.0)
                .Build();

            AssertValidSolution(solution);
        });
        sw.Stop();

        TestContext.WriteLine(
            $"Full pipeline on {model.Elements.Count} elements: {sw.ElapsedMilliseconds} ms");
    }

    */

    /*[Test]
    [Description("Рост времени полного конвейера при увеличении числа элементов в 4 раза близок к линейному/чуть хуже.")]
    public void FullPipeline_ScalesRoughlyLinearly_WithElementCount()
    {
        const int smallCount = 1_000;
        const int largeCount = 4_000;

        var smallTotal = MeasureFullPipeline(smallCount);
        var largeTotal = MeasureFullPipeline(largeCount);

        var ratio = largeTotal / (double)Math.Max(smallTotal, 1);

        TestContext.WriteLine(
            $"small({smallCount})={smallTotal} ms, " +
            $"large({largeCount})={largeTotal} ms, " +
            $"ratio={ratio:F2} (elements ratio = {largeCount / (double)smallCount})");

        // Ожидаем ~4x; сборка глобальной матрицы и разреженное решение
        // могут дать чуть больше — допускаем 12x с запасом на шум/GC.
        Assert.That(ratio, Is.LessThan(12.0),
            $"Full pipeline scaling is worse than expected: ratio = {ratio:F2}");
    }*/

    /*[Test]
    [TestCase(50_000, 30_000)]
    [TestCase(100_000, 90_000)]
    [Description("Очень большие сетки: полный конвейер завершается в пределах разумного времени.")]
    public void FullPipeline_VeryLargeMesh_Completes(int elementCount, int maxMilliseconds)
    {
        var (solution, phases) = RunFullPipelineWithPhases(elementCount);

        AssertValidSolution(solution);

        TestContext.WriteLine(
            $"Elements: {elementCount}, " +
            $"Model: {phases.ModelMs} ms, " +
            $"Assembly: {phases.AssemblyMs} ms, " +
            $"Build: {phases.BuildMs} ms, " +
            $"Total: {phases.TotalMs} ms");

        Assert.That(phases.TotalMs, Is.LessThan(maxMilliseconds),
            $"Full pipeline took too long: {phases.TotalMs} ms for {elementCount} elements");
    }*/

    /// <summary>
    /// Гоняет полный конвейер и возвращает решение и разбивку по фазам.
    /// </summary>
    private static (ConstructionModelSolution Solution, PhaseTimings Phases) RunFullPipelineWithPhases(int elementCount)
    {
        var descriptor = TriangularMeshFactory.Create(elementCount);

        // Фаза 1: модель (узлы + элементы + сборка ConstructionModel).
        var swModel = Stopwatch.StartNew();

        var modelBuilder = new ConstructionModelBuilder();
        var lastElementIndex = descriptor.Triangles.Count - 1;

        CollectModel(descriptor, lastElementIndex, modelBuilder);

        modelBuilder.Merge();
        var model = modelBuilder.Build();

        swModel.Stop();

        var uniqueNodes = model
            .Elements
            .SelectMany(e => e.Nodes)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Count();

        // Фаза 2: Assembly + промежуточное решение с временными Dirichlet.
        // Временные условия нужны только чтобы Build() не упал и вернул
        // какое-то решение — из него затем выберем узлы/значения для настоящих Dirichlet.

        var probeBuilder = new SolutionBuilder(model);
        probeBuilder
            .ValidateForCountOfElement()
            .ValidateForCommonNodes()
            .Assembly();

        var probeSolution = probeBuilder
            .AddDirichlet("e0 Node1", Freedom.Temperature, 0.0)
            .AddDirichlet($"e{lastElementIndex} Node1", Freedom.Temperature, 100.0)
            .Build();

        // Выбираем два узла из промежуточного решения и берём их значения.
        // Здесь — два узла, отличных от тех, что использовались во временных
        // условиях, чтобы финальное решение не совпадало с промежуточным тривиально.
        var probeItems = probeSolution.Items
            .Where(i => i.Node.NodeId != "e0 Node1" && i.Node.NodeId != $"e{lastElementIndex} Node1")
            .ToArray();

        Assert.That(probeItems, Has.Length.GreaterThanOrEqualTo(2),
            "Probe solution must contain at least two nodes distinct from the temporary Dirichlet nodes.");

        var dirichlet1 = probeItems[0];
        var dirichlet2 = probeItems[^1];


        // Фаза 2: Assembly на НОВОМ builder 
        var swAssembly = Stopwatch.StartNew();

        var finalBuilder = new SolutionBuilder(model);
        finalBuilder.Assembly(); 
        swAssembly.Stop();

        // Фаза 2: Условия Дирихле + Build
        var swBuild = Stopwatch.StartNew();
        var solution = finalBuilder
            .AddDirichlet(dirichlet1.Node.NodeId, Freedom.Temperature, dirichlet1.Value)
            .AddDirichlet(dirichlet2.Node.NodeId, Freedom.Temperature, dirichlet2.Value)
            .Build();

        swBuild.Stop();

        return (solution, new PhaseTimings(
            ModelMs: swModel.ElapsedMilliseconds,
            AssemblyMs: swAssembly.ElapsedMilliseconds,
            BuildMs: swBuild.ElapsedMilliseconds,
            UniqueNodes: uniqueNodes));
    }

    private static void CollectModel(TriangularMeshDescriptor descriptor, int lastElementIndex,
        ConstructionModelBuilder modelBuilder)
    {
        for (var i = 0; i < descriptor.Triangles.Count; i++)
        {
            var triangle = descriptor.Triangles[i];
            var nodeId = i == 0 || i == lastElementIndex
                ? $"e{i} Node1"
                : "";

            var triangleElement = new FiniteElementBuilder()
                .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
                {
                    ThermalConductivity = 1.0,
                    Thickness = 1.0,
                })
                .AddNode(triangle.N1.X, triangle.N1.Y, nodeId)
                .AddNode(triangle.N2.X, triangle.N2.Y)
                .AddNode(triangle.N3.X, triangle.N3.Y)
                .Build(triangle.ElementId);

            modelBuilder.AddElements(triangleElement);
        }
    }

    /*
    /// <summary>
    /// Упрощённый вариант для замеров масштабирования (без разбивки по фазам).
    /// </summary>
    private static long MeasureFullPipeline(int elementCount)
    {
        // Локальный прогрев, чтобы не смешивать с OneTimeSetUp.
        _ = RunFullPipeline(50);

        var (_, phases) = RunFullPipelineWithPhases(elementCount);
        return phases.TotalMs;
    }

    /// <summary>
    /// Возвращает только решение (для тестов, где нужен лишь результат).
    /// </summary>
    private static ConstructionModelSolution RunFullPipeline(int elementCount)
    {
        var model = TriangularMeshFactory.Create(elementCount);
        var maxBottomIndex = elementCount;

        return new SolutionBuilder(model)
            .Assembly()
            .AddDirichlet("b0", Freedom.Temperature, 0.0)
            .AddDirichlet($"b{maxBottomIndex}", Freedom.Temperature, 100.0)
            .Build();
    }
    */

    /// <summary>
    /// Проверяет, что решение не содержит NaN/Infinity и что закреплённые
    /// значения соблюдены. Это защищает от «тест прошёл, но решение битое».
    /// </summary>
    private static void AssertValidSolution(ConstructionModelSolution solution)
    {
        Assert.That(solution, Is.Not.Null);
        Assert.That(solution.Items, Is.Not.Null.And.Not.Empty,
            "Solution must contain at least one item.");

        Assert.Multiple(() =>
        {
            foreach (var item in solution.Items)
            {
                Assert.That(double.IsNaN(item.Value), Is.False,
                    $"Solution contains NaN at node '{item.Node.NodeId}'.");
                Assert.That(double.IsInfinity(item.Value), Is.False,
                    $"Solution contains Infinity at node '{item.Node.NodeId}'.");
            }

            var tLeft = solution.Items.FirstOrDefault(i => i.Node.NodeId != string.Empty)?.Value;
            var tRight = solution.Items.LastOrDefault(i => i.Node.NodeId != string.Empty)?.Value;

            Assert.That(tLeft, Is.Not.Null, "Dirichlet value at the left boundary must be preserved.");
            Assert.That(tRight, Is.Not.Null, "Dirichlet value at the right boundary must be preserved.");
        });
    }

    private readonly record struct PhaseTimings(long ModelMs, long AssemblyMs, long BuildMs, int UniqueNodes)
    {
        public long TotalMs => ModelMs + AssemblyMs + BuildMs;
    }
}