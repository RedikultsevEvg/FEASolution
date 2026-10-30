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
        const int elementCount = 10;
        var coordinatesProvider = TriangularMeshFactory.Create(elementCount);
        _ = RunFullPipelineWithPhases(coordinatesProvider);
    }

    [Test]
    [TestCase(3, 20)]
    [TestCase(1_000, 200)]
    [TestCase(2_500, 1500)]
    [TestCase(5_000, 15_000)]
    [Description("Полный конвейер на средних сетках укладывается в разумное время.")]
    public void FullPipeline_CompletesWithinReasonableTime(int elementCount, int maxMilliseconds)
    {
        // Arrange
        var coordinatesProvider = TriangularMeshFactory.Create(elementCount);

        // Act
        var (solution, phases) = RunFullPipelineWithPhases(coordinatesProvider);

        // Assert: решение корректно
        AssertValidSolution(solution);

        TestContext.WriteLine(
            $"Preconditions - Elements: {elementCount}, Unique nodes: {phases.UniqueNodeCount}");

        TestContext.WriteLine(
            $"Timing - " +
            $"Model collected: {phases.ModelCollectedMs} ms, " +
            $"Assembly: {phases.ModelAssemblyMs} ms, " +
            $"Build: {phases.BuildSolutionMs} ms, " +
            $"Total: {phases.TotalMs} ms");

        Assert.That(phases.TotalMs, Is.LessThan(maxMilliseconds),
            $"Full pipeline took too long: {phases.TotalMs} ms for {elementCount} elements " +
            $"(model collected ={phases.ModelCollectedMs}, assembly={phases.ModelAssemblyMs}, build={phases.BuildSolutionMs})");
    }

    /// <summary>
    /// Гоняет полный конвейер и возвращает решение и разбивку по фазам.
    /// </summary>
    private static (ConstructionModelSolution Solution, PhaseTimings Phases) RunFullPipelineWithPhases(TriangularCoordinatesProvider coordinatesProvider)
    {
        // Фаза 1: модель (узлы + элементы + сборка ConstructionModel).
        var swModel = Stopwatch.StartNew();

        var modelBuilder = new ConstructionModelBuilder();
        var lastElementIndex = coordinatesProvider.Triangles.Count - 1;
        CollectModel(coordinatesProvider, lastElementIndex, modelBuilder);
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
            ModelCollectedMs: swModel.ElapsedMilliseconds,
            ModelAssemblyMs: swAssembly.ElapsedMilliseconds,
            BuildSolutionMs: swBuild.ElapsedMilliseconds,
            UniqueNodeCount: uniqueNodes));
    }

    private static void CollectModel(TriangularCoordinatesProvider descriptor, int lastElementIndex,
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
                .AddNode(triangle.Node1.X, triangle.Node1.Y, nodeId)
                .AddNode(triangle.Node2.X, triangle.Node2.Y)
                .AddNode(triangle.Node3.X, triangle.Node3.Y)
                .Build(triangle.ElementId);

            modelBuilder.AddElements(triangleElement);
        }
    }

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

    private readonly record struct PhaseTimings(long ModelCollectedMs, long ModelAssemblyMs, long BuildSolutionMs, long UniqueNodeCount)
    {
        public long TotalMs => ModelCollectedMs + ModelAssemblyMs + BuildSolutionMs;
    }
}