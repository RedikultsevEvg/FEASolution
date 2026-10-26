using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests._2D;

/// <summary>
/// Интеграционные тесты задачи теплопроводности на сетке из 4 равнобедренных треугольников.
///
/// Геометрия.
/// ──────────
///   A(0,1),  B(2,2),  C(2,0)
///   D(4,1),  F(4,3),  G(6,2)
///
///            B(2,2) ______ F(4,3)
///            /| \          /  \
///           / |  \    T3  /    \
///          /  |   \      /  T4  \
///   A(0,1)  T1| T2 D(4,1) ------- G(6,2)
///          \  |   /      
///           \ |  /
///            \| /
///            C(2,0)     
///
/// Топология.
/// ──────────
///   T1 = A, B, C
///   T2 = B, C, D
///   T3 = B, D, F      ← ребро BD — общая диагональ T2 и T3
///   T4 = D, F, G 
///
/// Общие узлы:
///   B,  — T1, T2, T3;
///   C,  — T1, T2;
///   D   — T2, T3, T4;
///   F   — T3, T4.
///
/// Уникальных узлов: 6 (A, B, C, D, F, G).
///
/// Свойства: k = t = 1 на всех элементах, без источников.
/// Граничные условия Дирихле прикладываются к двум узлам.
///
/// Для этой геометрии полная аналитическая формула решения громоздка
/// (сетка не прямоугольная по x: F и G выше горизонтали A–D).
/// Тесты опираются на:
///   • точные значения в закреплённых узлах;
///   • принцип максимума;
///   • вырожденные случаи с точным ответом (равные граничные → константа,
///     два узла на одной горизонтали → линейный профиль на этой линии);
///   • детерминированность, конечность, состав узлов;
///   • независимость от k, t при однородных свойствах.
/// </summary>
[TestFixture]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class FourIsoscelesTrianglesIntegrationTests
{
    private const double Tolerance = 1e-9;

    private static readonly string[] AllNodeIds =
        ["nodeA", "nodeB", "nodeC", "nodeD", "nodeF", "nodeG"];

    [TestCase("nodeA", "nodeB", 20.0, 100.0,
        new[] { 20.0, 100.0, 83.105590062111801d, 95.527950310558992d, 96.521739130434753d, 96.024844720496873d })]
    [TestCase("nodeA", "nodeC", 20.0, 100.0,
        new[] { 20.0, 83.105590062111773d, 100.0, 87.577639751552752d, 86.583850931676977d, 87.080745341614872d })]
    [TestCase("nodeB", "nodeG", 20.0, 100.0,
        new[] { 22.857142857142851d, 20.0, 25.714285714285715d, 45.714285714285708d, 51.428571428571416d, 100.0 })]
    [TestCase("nodeB", "nodeD", 20.0, 100.0,
        new[] { 28.888888888888889d, 20.0, 37.777777777777786d, 100.0, 82.222222222222229d, 91.111111111111114d })]
    [TestCase("nodeB", "nodeC", 20.0, 100.0,
        new[] { 60.0, 20.0, 100.0, 41.17647058823529d, 36.470588235294116d, 38.823529411764703d })]
    [TestCase("nodeB", "nodeF", 20.0, 100.0,
        new[] { 24.955752212389381d, 20.0, 29.911504424778766d, 64.601769911504434d, 100.0, 82.300884955752224d })]
    [TestCase("nodeC", "nodeD", 20.0, 100.0,
        new[] { 37.69911504424779d, 55.398230088495588d, 20.0, 100.0, 90.088495575221245d, 95.04424778761063d })]
    [TestCase("nodeC", "nodeF", 20.0, 100.0,
        new[] { 34.117647058823529d, 48.235294117647058d, 20.0, 71.764705882352914d, 100.0, 85.88235294117645d })]
    [TestCase("nodeD", "nodeG", 20.0, 100.0,
        new[] { 23.975155279503113d, 24.472049689441004d, 23.478260869565226d, 20.0, 36.894409937888206d, 100.0 })]
    [TestCase("nodeD", "nodeF", 20.0, 100.0,
        new[] { 38.82352941176471d, 41.176470588235304d, 36.47058823529413d, 20.0, 100.0, 60.0 })]
    [TestCase("nodeA", "nodeG", 20.0, 80.0,
        new[] {  20.0, 44.878048780487788d, 41.951219512195117d, 55.121951219512184d, 58.04878048780487, 80.0 })]
    [TestCase("nodeA", "nodeD", 20.0, 100.0,
        new[] {  20.0, 74.285714285714278d, 68.571428571428569d, 100.0, 94.28571428571432d, 97.142857142857153d })]
    [TestCase("nodeA", "nodeF", 20.0, 100.0,
        new[] {  20.0, 67.384615384615373d, 61.230769230769226d, 84.0d, 100.0, 92.0d })]
    [TestCase("nodeC", "nodeG", 100.0, 20.0,
        new[] { 92.0d, 84.0d, 100.0, 67.384615384615373d, 61.230769230769226d, 20.0 })]
    [TestCase("nodeF", "nodeG", 20.0, 100.0,
        new[] {  32.919254658385093d,  32.422360248447212d,  33.416149068322987d,  36.894409937888206d, 20.0, 100.0 })]
    public void ExactValues_ForEveryBoundaryPair(
        string node1,
        string node2,
        double tempNode1,
        double tempNode2,
        double[] expectedByNodeId)
    {
        Assert.That(expectedByNodeId, Has.Length.EqualTo(AllNodeIds.Length),
            "Массив ожидаемых значений должен содержать по одному значению на узел.");

        var solution = SolveBetween(node1, tempNode1, node2, tempNode2);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, node1), Is.EqualTo(tempNode1));
            Assert.That(ValueOf(solution, node2), Is.EqualTo(tempNode2));

            for (var i = 0; i < AllNodeIds.Length; i++)
            {
                var nodeId = AllNodeIds[i];

                Assert.That(ValueOf(solution, nodeId), Is.EqualTo(expectedByNodeId[i]).Within(Tolerance),
                    $"T({nodeId}) ожидалось {expectedByNodeId[i]}, получено {ValueOf(solution, nodeId)} ");
            }
        });
    }

    [Test]
    public void SolutionVector_ContainsExactlySixDistinctNodeIds()
    {
        var solution = SolveBetween("nodeA", 20.0, "nodeG", 100.0);

        var ids = solution.Items.Select(i => i.Node.NodeId).ToList();

        Assert.That(ids, Has.Count.EqualTo(6),
            "В модели 6 уникальных узлов; вектор решения не должен содержать дубликатов.");
        Assert.That(ids, Is.EquivalentTo(AllNodeIds));
    }

    [Test]
    public void SolutionVector_AllItems_HaveTemperatureFreedom()
    {
        var solution = SolveBetween("nodeA", 20.0, "nodeG", 100.0);

        Assert.That(solution.Items, Is.Not.Empty);
        Assert.That(solution.Items.Select(i => i.Freedom).Distinct(),
            Is.EquivalentTo([Freedom.Temperature]));
    }

    [Test]
    public void SharedNodes_AreSingleInstancesAcrossElements()
    {
        var t1 = BuildElementT1();
        var t2 = BuildElementT2(t1);
        var t3 = BuildElementT3(t2);
        var t4 = BuildElementT4(t3);

        Assert.Multiple(() =>
        {
            Assert.That(t2.Node("nodeB"), Is.SameAs(t1.Node("nodeB")));
            Assert.That(t2.Node("nodeB"), Is.SameAs(t3.Node("nodeB")));

            Assert.That(t2.Node("nodeC"), Is.SameAs(t1.Node("nodeC")));

            Assert.That(t3.Node("nodeD"), Is.SameAs(t2.Node("nodeD")));
            Assert.That(t4.Node("nodeD"), Is.SameAs(t2.Node("nodeD")));

            Assert.That(t4.Node("nodeF"), Is.SameAs(t3.Node("nodeF")));
        });
    }

    [Test]
    public void Model_HasTwelveNodesAndSixUnique()
    {
        var model = BuildModel();

        var allNodes = model.Elements.SelectMany(e => e.Nodes).ToList();
        Assert.That(allNodes, Has.Count.EqualTo(12));

        var unique = allNodes.Distinct(ReferenceEqualityComparer.Instance).ToList();
        Assert.That(unique, Has.Count.EqualTo(6));
    }

    [TestCase(0.0, 100.0)]
    [TestCase(20.0, 100.0)]
    [TestCase(-50.0, 50.0)]
    [TestCase(100.0, -100.0)]
    [TestCase(273.15, 373.73)]
    public void TheNodesABWithDirichlet_AreExactValues(double tempA, double tempG)
    {
        var solution = SolveBetween("nodeA", tempA, "nodeG", tempG);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeG"), Is.EqualTo(tempG));
        });
    }

    [TestCase(0.0, 100.0)]
    [TestCase(-10.0, 10.0)]
    public void TheNodesCGWithDirichlet_AreExactValues(double tempC, double tempG)
    {
        var solution = SolveBetween("nodeC", tempC, "nodeG", tempG);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(tempC));
            Assert.That(ValueOf(solution, "nodeG"), Is.EqualTo(tempG));
        });
    }

    [TestCase(0.0, 100.0)]
    [TestCase(30.0, 70.0)]
    public void TheNodesBFWithDirichlet_AreExactValues(double tempB, double tempF)
    {
        var solution = SolveBetween("nodeB", tempB, "nodeF", tempF);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(tempB));
            Assert.That(ValueOf(solution, "nodeF"), Is.EqualTo(tempF));
        });
    }

    [TestCase(0.0, 100.0)]
    [TestCase(50.0, 50.0)]
    public void TheNodesADWithDirichlet_AreExactValues(double tempA, double tempD)
    {
        var solution = SolveBetween("nodeA", tempA, "nodeD", tempD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Принцип максимума и физические инварианты
    // ────────────────────────────────────────────────────────────────

    [TestCase("nodeA", 20.0, "nodeG", 100.0)]
    [TestCase("nodeC", 0.0, "nodeG", 100.0)]
    [TestCase("nodeB", -30.0, "nodeF", 70.0)]
    public void PrincipleOfMaximum_HoldsForAllNodes(
        string node1, double value1, string node2, double value2)
    {
        var solution = SolveBetween(node1, value1, node2, value2);

        var min = Math.Min(value1, value2);
        var max = Math.Max(value1, value2);

        foreach (var item in solution.Items)
        {
            Assert.That(item.Value, Is.InRange(min - Tolerance, max + Tolerance),
                $"Узел '{item.Node.NodeId}' вышел за пределы [{min}, {max}]: {item.Value}.");
        }
    }
    
    [Test]
    public void AllValues_AreFinite()
    {
        var solution = SolveBetween("nodeA", 20.0, "nodeG", 100.0);

        foreach (var item in solution.Items)
        {
            Assert.That(double.IsFinite(item.Value), Is.True,
                $"Узел '{item.Node.NodeId}' получил нефинитное значение: {item.Value}.");
        }
    }

    [TestCase(42.0)]
    [TestCase(0.0)]
    [TestCase(-100.0)]
    public void EqualDirichletValues_ProduceConstantSolution(double t)
    {
        var solution = SolveBetween("nodeA", t, "nodeG", t);

        foreach (var item in solution.Items)
        {
            Assert.That(item.Value, Is.EqualTo(t).Within(Tolerance));
        }
    }

    [Test]
    public void Solution_GrowsAlongX_OnBottomAndTopLines()
    {
        const double tempA = 0.0;
        const double tempG = 100.0;

        var solution = SolveBetween("nodeA", tempA, "nodeG", tempG);

        Assert.That(ValueOf(solution, "nodeC"),
            Is.LessThan(ValueOf(solution, "nodeG")));

        Assert.That(ValueOf(solution, "nodeB"),
            Is.LessThan(ValueOf(solution, "nodeD")));

        Assert.That(ValueOf(solution, "nodeD"),
            Is.LessThan(ValueOf(solution, "nodeF")));
    }

    [TestCase(1.0, 1.0)]
    [TestCase(2.0, 1.0)]
    [TestCase(1.0, 2.0)]
    [TestCase(5.0, 5.0)]
    [TestCase(0.1, 0.1)]
    public void Solution_Independent_OfConductivityAndThickness(double k, double t)
    {
        const double tempA = 20.0;
        const double tempG = 100.0;

        var reference = SolveBetween("nodeA", tempA, "nodeG", tempG);
        var withParams = SolveBetweenWithParams("nodeA", tempA, "nodeG", tempG, k, t);

        foreach (var nodeId in AllNodeIds)
        {
            Assert.That(ValueOf(withParams, nodeId),
                Is.EqualTo(ValueOf(reference, nodeId)).Within(Tolerance),
                $"Значение в узле '{nodeId}' не должно зависеть от k = {k}, t = {t}.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Детерминированность и негативы
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Solution_IsDeterministic_AcrossRuns()
    {
        var first = SolveBetween("nodeA", 20.0, "nodeG", 100.0);
        var second = SolveBetween("nodeA", 20.0, "nodeG", 100.0);

        foreach (var nodeId in AllNodeIds)
        {
            Assert.That(ValueOf(second, nodeId),
                Is.EqualTo(ValueOf(first, nodeId)),
                $"Значение в узле '{nodeId}' нестабильно между запусками.");
        }
    }

    [Test]
    public void Build_WithoutDirichlet_Throws()
    {
        var builder = new SolutionBuilder(BuildModel()).Assembly();

        var ex = Assert.Throws<FeaSolutionBuilderException>(() => builder.Build());

        Assert.That(ex!.Message, Is.EqualTo(
            "Dirichlet condition collection is empty. Call 'AddDirichlet' first."));
    }

    [Test]
    public void Build_BeforeAssembly_Throws()
    {
        var builder = new SolutionBuilder(BuildModel());

        var ex = Assert.Throws<FeaSolutionBuilderException>(() => builder.Build());

        Assert.That(ex!.Message, Is.EqualTo(
            "Construction model stiffness matrix is not assembled. Call 'Assembly' first."));
    }

    [Test]
    public void Build_WithSingleDirichlet_DoesNotThrowButMayFailInSolver()
    {
        var builder = new SolutionBuilder(BuildModel())
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, 20.0);

        Assert.DoesNotThrow(() => builder.Build());
    }

    #region Helpers

    private static TemperatureElementOptions Options(double k = 1.0, double t = 1.0) =>
        new() { ThermalConductivity = k, Thickness = t };

    /// <summary>
    /// T1 = ABC: A(0,1), B(2,2), C(2,0).
    /// </summary>
    private static IFiniteElement BuildElementT1(double k = 1.0, double t = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options(k, t))
            .AddNode(0.0, 1.0, "nodeA")
            .AddNode(2.0, 2.0, "nodeB")
            .AddNode(2.0, 0.0, "nodeC")
            .Build("T1: A, B, C");

    /// <summary>
    /// T2 = BCD: B(2,2), C(2,0), D(4,1). B, C — из T1.
    /// </summary>
    private static IFiniteElement BuildElementT2(IFiniteElement t1, double k = 1.0, double t = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options(k, t))
            .AddNode(t1.Node("nodeB"))
            .AddNode(t1.Node("nodeC"))
            .AddNode(4.0, 1.0, "nodeD")
            .Build("T2: B, C, D");

    /// <summary>
    /// T3 = BDF: B(2,2), D(4,1), F(4,3). B и D — из T2.
    /// </summary>
    private static IFiniteElement BuildElementT3(IFiniteElement t2, double k = 1.0, double t = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options(k, t))
            .AddNode(t2.Node("nodeB"))
            .AddNode(t2.Node("nodeD"))
            .AddNode(4.0, 3.0, "nodeF")
            .Build("T3: B, D, F");

    /// <summary>
    /// T4 = FGH: F(4,3), G(6,2), H(6,0). F — из T3.
    /// </summary>
    private static IFiniteElement BuildElementT4(IFiniteElement t3, double k = 1.0, double t = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options(k, t))
            .AddNode(t3.Node("nodeF"))
            .AddNode(t3.Node("nodeD"))
            .AddNode(6.0, 2.0, "nodeG")
            .Build("T4: F, D, G");

    /// <summary>
    /// Модель из 4 треугольников с общими свойствами k, t.
    /// </summary>
    private static IConstructionModel BuildModel(double k = 1.0, double t = 1.0)
    {
        var triangle1 = BuildElementT1(k, t);
        var triangle2 = BuildElementT2(triangle1, k, t);
        var triangle3 = BuildElementT3(triangle2, k, t);
        var triangle4 = BuildElementT4(triangle3, k, t);

        return new ConstructionModelBuilder()
            .AddElements(triangle1, triangle2, triangle3, triangle4)
            .Build();
    }

    private static ConstructionModelSolution SolveBetween(
        string node1, double value1, string node2, double value2) =>
        new SolutionBuilder(BuildModel())
            .Assembly()
            .AddDirichlet(node1, Freedom.Temperature, value1)
            .AddDirichlet(node2, Freedom.Temperature, value2)
            .Build();

    private static ConstructionModelSolution SolveBetweenWithParams(
        string node1, double value1, string node2, double value2,
        double k, double t) =>
        new SolutionBuilder(BuildModel(k, t))
            .Assembly()
            .AddDirichlet(node1, Freedom.Temperature, value1)
            .AddDirichlet(node2, Freedom.Temperature, value2)
            .Build();

    private static double ValueOf(ConstructionModelSolution solution, string nodeId)
    {
        var item = solution.Items.SingleOrDefault(i => i.Node.NodeId == nodeId);
        Assert.That(item, Is.Not.Null, $"В решении отсутствует узел '{nodeId}'.");
        return item!.Value;
    }

    #endregion
}