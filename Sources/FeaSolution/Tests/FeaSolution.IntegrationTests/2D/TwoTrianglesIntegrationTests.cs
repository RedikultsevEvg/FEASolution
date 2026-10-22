using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests._2D;

/// <summary>
/// Интеграционный тест по примеру из <c>Model2DTwoTriangles</c>.
/// Проверяет весь путь: построение элементов → сборка глобальной матрицы →
/// применение граничных условий Дирихле → решение СЛАУ.
/// </summary>
[TestFixture]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class TwoTrianglesIntegrationTests
{
    // Геометрия из примера:
    //   nodeA(0,1) ---- nodeB(2,2)
    //      |          /      \
    //      |   T1   /    T2    \
    //      |      /              \
    //   nodeC(2,0) ------------ nodeD(4,1)
    //
    // Граничные условия:
    //   T(nodeA) = 20
    //   T(nodeD) = 100
    //
    // Проверяем, что решение:
    //   - определено для всех 4 уникальных узлов,
    //   - строго между граничными значениями в промежуточных узлах,
    //   - конечно,
    //   - детерминировано (повторный запуск даёт тот же результат).

    private const double TemperatureNodeA = 20.0;
    private const double TemperatureNodeD = 100.0;

    [Test]
    public void TwoTriangles_FullPipeline_ProducesSolutionForAllUniqueNodes()
    {
        // Act
        var solution = Solve();

        // Assert
        var nodeIds = solution.Items
            .Select(i => i.Node.NodeId)
            .ToList();

        Assert.That(nodeIds, Does.Contain("nodeA"));
        Assert.That(nodeIds, Does.Contain("nodeB"));
        Assert.That(nodeIds, Does.Contain("nodeC"));
        Assert.That(nodeIds, Does.Contain("nodeD"));
    }

    [Test]
    public void TwoTriangles_Solution_SatisfiesDirichletValues()
    {
        // Act
        var solution = Solve();

        // Assert
        var atA = ValueOf(solution, "nodeA");
        var atD = ValueOf(solution, "nodeD");

        Assert.That(atA, Is.EqualTo(TemperatureNodeA),
            $"Узел A должен принять заданное граничное значение {TemperatureNodeA}.");
        Assert.That(atD, Is.EqualTo(TemperatureNodeD),
            $"Узел D должен принять заданное граничное значение {TemperatureNodeD}.");
    }

    [Test]
    public void TwoTriangles_IntermediateNodes_AreBetweenBoundaryValues()
    {
        // Act
        var solution = Solve();

        var atB = ValueOf(solution, "nodeB");
        var atC = ValueOf(solution, "nodeC");

        // Assert
        // для задачи теплопроводности без внутренних источников
        // решение удовлетворяет принципу максимума:
        // min(граница) <= T(x) <= max(граница) во всех внутренних узлах.
        Assert.That(atB, Is.GreaterThanOrEqualTo(TemperatureNodeA - 1e-9));
        Assert.That(atB, Is.LessThanOrEqualTo(TemperatureNodeD + 1e-9));
        Assert.That(atC, Is.GreaterThanOrEqualTo(TemperatureNodeA - 1e-9));
        Assert.That(atC, Is.LessThanOrEqualTo(TemperatureNodeD + 1e-9));
    }

    [Test]
    public void TwoTriangles_AllValues_AreFinite()
    {
        // Act
        var solution = Solve();

        // Assert
        // ни один узел не должен получить NaN/Infinity.
        Assert.That(solution.Items, Is.Not.Empty);
        foreach (var item in solution.Items)
        {
            Assert.That(double.IsFinite(item.Value), Is.True,
                $"Узел '{item.Node.NodeId}' получил нефинитное значение: {item.Value}.");
        }
    }

    [Test]
    public void TwoTriangles_Solution_IsDeterministic()
    {
        // Act
        var first = Solve();
        var second = Solve();

        // Assert
        //
        // повторный запуск на идентичной модели должен дать
        // побитово равные значения во всех узлах (детерминированная сборка
        // + детерминированный решатель).
        foreach (var nodeId in new[] { "nodeA", "nodeB", "nodeC", "nodeD" })
        {
            Assert.That(ValueOf(second, nodeId),
                Is.EqualTo(ValueOf(first, nodeId)).Within(1e-12),
                $"Значение в узле '{nodeId}' нестабильно между запусками.");
        }
    }

    [Test]
    public void TwoTriangles_Solution_ContainsOnlyTemperatureFreedom()
    {
        // Act
        var solution = Solve();

        // Assert
        // в модели задана только Freedom.Temperature.
        Assert.That(solution.Items.Select(i => i.Freedom).Distinct(),
            Is.EquivalentTo([Freedom.Temperature]));
    }

    [Test]
    public void TwoTriangles_WithoutDirichlet_Throws()
    {
        // Arrange
        var model = BuildModel();
        var builder = new SolutionBuilder(model).Assembly();

        // Act
        var ex = Assert.Throws<FeaSolutionBuilderException>(
             () => builder.Build());

        // Assert —
        // без граничных условий матрица вырождена,
        Assert.That(ex!.Message, Is.EqualTo("Dirichlet condition collection is empty. Call 'AddDirichlet' first."));
    }

    #region Helpers

    private static double ValueOf(ConstructionModelSolution solution, string nodeId)
    {
        var item = solution.Items.SingleOrDefault(i => i.Node.NodeId == nodeId);
        Assert.That(item, Is.Not.Null, $"В решении отсутствует узел '{nodeId}'.");
        return item!.Value;
    }

    private static IFiniteElement BuildElement1() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(0, 1.0, "nodeA")
            .AddNode(2.0, 2.0, "nodeB")
            .AddNode(2.0, 0, "nodeC")
            .Build("The first triangle");

    private static IFiniteElement BuildElement2(IFiniteElement element1) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(element1.Node("nodeB"))
            .AddNode(element1.Node("nodeC"))
            .AddNode(4.0, 1.0, "nodeD")
            .Build("The second triangle");

    private static IConstructionModel BuildModel()
    {
        var element1 = BuildElement1();
        var element2 = BuildElement2(element1);

        return new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2)
            .Build();
    }

    private static ConstructionModelSolution Solve()
    {
        var model = BuildModel();

        return new SolutionBuilder(model)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, TemperatureNodeA)
            .AddDirichlet("nodeD", Freedom.Temperature, TemperatureNodeD)
            .Build();
    }

    #endregion
}