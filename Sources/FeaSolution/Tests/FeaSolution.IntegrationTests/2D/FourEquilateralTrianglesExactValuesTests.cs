using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests._2D;

/// <summary>
/// Интеграционные тесты задачи теплопроводности на сетке из 4 РАВНОСТОРОННИХ
/// треугольников (все стороны каждого = 4).
///
/// Геометрия.
/// ──────────
///   A(2, -2√3),  B(0, 0),   C(4, 0)
///   D(2,  2√3),  F(-2, 2√3), G(0, 4√3)
///
///                         G(0, 4√3)
///                          /\
///                         /  \
///                        / T4 \
///                       /      \
///            F(-2,2√3)---------D(2,2√3)
///                      \       / \
///                       \ T3  /T2 \
///                        \   /     \
///                         \ /       \
///                     B(0,0)--------C(4,0)
///                           \       /
///                            \ T1  /
///                             \   /
///                              \ /
///                               \/
///                           A(2,-2√3)
///
/// Топология.
/// ──────────
///   T1 = A, B, C
///   T2 = B, C, D
///   T3 = B, D, F
///   T4 = D, F, G
///
/// Общие узлы:
///   B — T1, T2, T3
///   C — T1, T2
///   D — T2, T3, T4
///   F — T3, T4
///
/// Проверка равносторонности (все стороны = 4):
///   T1: |AB| = |AC| = |BC| = 4
///   T2: |BC| = |BD| = |CD| = 4
///   T3: |BD| = |BF| = |DF| = 4
///   T4: |DF| = |DG| = |FG| = 4
///
/// Свойства: k = t = 1, без источников.
/// Dirichlet прикладывается к двум узлам.
///
/// Ожидаемые значения (expectedByNodeId) в порядке AllNodeIds:
///   [T(A), T(B), T(C), T(D), T(F), T(G)].
/// </summary>
[TestFixture]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class FourEquilateralTrianglesExactValuesTests
{
    private const double Tolerance = 1e-9;

    /// <summary>
    /// Высота равностороннего треугольника со стороной 4: 4·√3/2 = 2√3.
    /// </summary>
    private static readonly double H = 2.0 * Math.Sqrt(3.0);

    private static readonly string[] AllNodeIds =
        ["nodeA", "nodeB", "nodeC", "nodeD", "nodeF", "nodeG"];

    // ────────────────────────────────────────────────────────────────
    // Точные значения для всех 15 пар закреплённых узлов
    // ────────────────────────────────────────────────────────────────
    //
    // Порядок массива expectedByNodeId:
    //   [T(A), T(B), T(C), T(D), T(F), T(G)]
    //
    // Эталонные значения для пар с (20, 100). Для других пар значений
    // (например, (100, 20), (0, 1), (-50, 50)) — добавьте дополнительные
    // [TestCase], предварительно сняв фактические значения из решателя.

    [TestCase("nodeA", "nodeB", 20.0, 100.0,
        new[] { 20.0, 100.0, 78.556701030927826d, 94.226804123711347d, 95.876288659793829d, 95.051546391752609d })]
    [TestCase("nodeA", "nodeC", 20.0, 100.0,
        new[] { 20.0, 78.556701030927826d, 100.0, 84.329896907216522d, 82.680412371134054d, 83.505154639175302d })]
    [TestCase("nodeA", "nodeD", 20.0, 100.0,
        new[] { 20.0, 74.545454545454575d, 67.272727272727309d, 100.0, 92.727272727272748d, 96.363636363636374d })]
    
    [TestCase("nodeA", "nodeF", 20.0, 100.0,
        new[] { 20.0, 66, 58, 80, 100.0, 90 })]
    [TestCase("nodeA", "nodeF", -50.0, 50.0,
        new[] { -50, 7.5, -2.5, 25, 50.0, 37.5 })]
    [TestCase("nodeA", "nodeF", 50.0, -50.0,
        new[] { 50, -7.5, 2.5, -25, -50.0, -37.5 })]
   
    [TestCase("nodeA", "nodeG", 20.0, 100.0,
        new[] { 20.0, 53.548387096774242d, 48.38709677419358d, 66.451612903225836d, 71.612903225806491d, 100.0 })]
    [TestCase("nodeB", "nodeC", 20.0, 100.0,
        new[] { 60, 20.0, 100.0, 41.538461538461561d, 35.384615384615415d, 38.461538461538495d })]
    [TestCase("nodeB", "nodeD", 20.0, 100.0,
        new[] { 31.428571428571427d, 20.0, 42.857142857142868d, 100.0, 77.142857142857125d, 88.571428571428555d })]
    [TestCase("nodeB", "nodeF", 20.0, 100.0,
        new[] { 25.479452054794518d, 20.0, 30.958904109589046d, 58.356164383561634d, 100.0, 79.178082191780803d })]
    [TestCase("nodeB", "nodeG", 20.0, 100.0,
        new[] { 23.636363636363633d, 20.0, 27.27272727272728d, 45.45454545454546d, 52.727272727272734d, 100.0 })]
    [TestCase("nodeC", "nodeD", 20.0, 100.0,
        new[] { 40.82191780821919d, 61.643835616438388d, 20.0, 100.0, 89.041095890411d, 94.520547945205493d })]

    [TestCase("nodeC", "nodeF", 20.0, 100.0,
        new[] { 36, 52, 20.0, 68, 100.0, 84 })]
    [TestCase("nodeC", "nodeF", -40.0, 60.0,
        new[] { -20, 0, -40.0, 20, 60.0, 40 })]
    [TestCase("nodeC", "nodeF", 40.0, -60.0,
        new[] { 20, 0, 40.0, -20, -60.0, -40 })]

    [TestCase("nodeC", "nodeG", 20.0, 100.0,
        new[] { 30, 40, 20.0, 54, 62, 100.0 })]
    [TestCase("nodeC", "nodeG", 50.0, -50.0,
        new[] { 37.5, 25, 50.0, 7.5, -2.5, -50.0 })]
    [TestCase("nodeC", "nodeG", -50.0, 50.0,
        new[] { -37.5, -25, -50.0, -7.5, 2.5, 50.0 })]

    [TestCase("nodeD", "nodeF", 20.0, 100.0,
        new[] { 38.461538461538474d, 41.538461538461561d, 35.384615384615401d, 20.0, 100.0, 60 })]
    [TestCase("nodeD", "nodeG", 20.0, 100.0,
        new[] { 24.948453608247426d, 25.773195876288675d, 24.123711340206192d, 20.0, 41.443298969072174d, 100.0 })]
    [TestCase("nodeF", "nodeG", 20.0, 100.0,
        new[] { 36.494845360824776d, 35.670103092783542d, 37.319587628866017d, 41.443298969072195d, 20.0, 100.0 })]
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
                    $"T({nodeId}) ожидалось {expectedByNodeId[i]}, " +
                    $"получено {ValueOf(solution, nodeId)} ");
            }
        });
    }

    private static TemperatureElementOptions Options() =>
        new() { ThermalConductivity = 1.0, Thickness = 1.0 };

    /// <summary>
    /// T1 = A(2, -H), B(0, 0), C(4, 0). Все стороны = 4.
    /// </summary>
    private static IFiniteElement BuildElementT1() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options())
            .AddNode(2.0, -H, "nodeA")
            .AddNode(0.0, 0.0, "nodeB")
            .AddNode(4.0, 0.0, "nodeC")
            .Build("T1: A, B, C");

    /// <summary>
    /// T2 = B(0, 0), C(4, 0), D(2, H). Все стороны = 4.
    /// B, C — из T1.
    /// </summary>
    private static IFiniteElement BuildElementT2() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options())
            .AddNode(0.0, 0.0)
            .AddNode(4.0, 0.0)
            .AddNode(2.0, H, "nodeD")
            .Build("T2: B, C, D");

    /// <summary>
    /// T3 = B(0, 0), D(2, H), F(-2, H). Все стороны = 4.
    /// B — из T1, D — из T2.
    /// </summary>
    private static IFiniteElement BuildElementT3() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options())
            .AddNode(0.0, 0.0)
            .AddNode(2.0, H)
            .AddNode(-2.0, H, "nodeF")
            .Build("T3: B, D, F");

    /// <summary>
    /// T4 = D(2, H), F(-2, H), G(0, 2H). Все стороны = 4.
    /// D — из T2, F — из T3.
    /// </summary>
    private static IFiniteElement BuildElementT4() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, Options())
            .AddNode(2.0, H)
            .AddNode(-2.0, H)
            .AddNode(0.0, 2.0 * H, "nodeG")
            .Build("T4: D, F, G");

    private static IConstructionModel BuildModel()
    {
        var t1 = BuildElementT1();
        var t2 = BuildElementT2();
        var t3 = BuildElementT3();
        var t4 = BuildElementT4();

        return new ConstructionModelBuilder()
            .AddElements(t1, t2, t3, t4)
            .Merge()
            .Build();
    }

    private static ConstructionModelSolution SolveBetween(
        string node1, double value1, string node2, double value2) =>
        new SolutionBuilder(BuildModel())
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
}