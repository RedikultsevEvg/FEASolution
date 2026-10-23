using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests._2D;

/// <summary>
/// Интеграционные тесты влияния координат узлов на решение
/// задачи теплопроводности на двух треугольниках.
///
/// Базовая геометрия.
/// ───────────────────
///   T1: A(0,1), B(2,2), C(2,0)
///   T2: B(2,2), C(2,0), D(4,1)
///
///            B(2,2)
///            / | \
///           /  |  \
///          /   |   \
/// A(0,1)  /    |    \ D(4,1)
///         \    |    /
///          \   |   /
///           \  |  /
///            \ | /
///            C(2,0)
///
/// Свойства k = t = 1 на обоих треугольниках, Dirichlet в A и D.
/// Для такой однородной задачи без источников решение линейно по x
/// в каждой горизонтали, если ребро BC остаётся вертикальным,
/// а A и D — на одной горизонтали.
///
/// Тесты этого класса проверяют, что:
///   • при сдвиге B по Y меняется форма T1, что ломает симметрию
///     T(B) = T(C) — это индикатор учёта координаты y;
///   • при сдвиге D по X ребро BC остаётся вертикальным, и решение
///     удовлетворяет точной формуле T(B) = T(C) = T_A + (T_D−T_A)·2/x_D;
///   • монотонность и граничные значения сохраняются при любых сдвигах.
/// </summary>
[TestFixture]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class TwoTrianglesCoordinatesIntegrationTests
{
    private const double Tolerance = 1e-9;

    // ────────────────────────────────────────────────────────────────
    // Сдвиг узла B по оси Y
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Сдвиг узла B вдоль оси Y (x_B = 2 фиксирован, y_B меняется).
    ///
    /// Аналитическое обоснование.
    /// ────────────────────────────
    /// Граничные условия заданы только в A(0,1) и D(4,1) — обе точки
    /// на горизонтали y = 1. Функция T(x,y) = T_A + (T_D − T_A)·x/4
    /// удовлетворяет уравнению Лапласа (линейна), принимает T_A в A и T_D
    /// в D. Для линейных треугольных КЭ линейная функция воспроизводится
    /// точно, поэтому в узлах B и C:
    ///
    ///   T(B) = T(C) = T_A + (T_D − T_A) · 2 / 4 = середина профиля.
    ///
    /// Это верно для ЛЮБОГО y_B: сдвиг B по Y меняет форму T1 и T2, но
    /// не меняет единственное решение краевой задачи.
    /// </summary>
    [TestCase(1.0)]    // базовая симметрия
    [TestCase(0.5)]    // B опущен
    [TestCase(2.0)]    // B поднят
    [TestCase(3.0)]    // B сильно поднят
    [TestCase(-1.0)]   // B ниже C
    [TestCase(0.1)]    // B почти на уровне C
    [TestCase(5.0)]    // B далеко вверху
    [TestCase(-5.0)]   // B далеко внизу
    public void MoveNodeB_AlongY_DoesNotChangeTheSolution(double yCoordinate)
    {
        const double tempA = 20.0;
        const double tempD = 100.0;
        const double expectedB = 60; // tempA + (tempD - tempA) * 0.5;  
        const double expectedC = expectedB;

        var solution = SolveWithNodeB(yCoordinate, tempA, tempD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));

            // Сдвиг узла B по Y не меняет решение: линейный профиль по x.
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedB).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedC).Within(Tolerance));
        });
    }

    /// <summary>
    /// Эталонный случай: y_B = 1 совпадает с y_A = y_D. Геометрия
    /// симметрична по y, T(B) = T(C) = середине профиля.
    /// </summary>
    [Test]
    public void MoveNodeB_AtSymmetryLine_SolutionIsAtMiddle()
    {
        const double tempA = 20.0;
        const double tempD = 100.0;
        const double expected = 60.0; // 20 + (100 − 20)·0.5

        var solution = SolveWithNodeB(yCoordinate: 1.0, tempA, tempD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expected).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expected).Within(Tolerance));
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Сдвиг узла D по оси X
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Сдвиг D вдоль горизонтали y = 1 (x_D меняется, y_D = 1).
    /// Ребро BC на x = 2, узел A на x = 0. При однородных k, t
    /// решение линейно по x:
    ///
    ///   T(B) = T(C) = T_A + (T_D − T_A) · 2 / x_D.
    ///
    /// Точная формула — жёсткая проверка учёта x-координаты D.
    /// </summary>
    [TestCase(4.0, 100.0/2)] // равные треугольники
    [TestCase(5.0, 40.0)]
    [TestCase(6.0, 33.333333333333336d)]     
    [TestCase(8.0, 100.0/4 )] // второй треугольник в 3 раза шире
    [TestCase(10.0, 100.0/5 )] // второй треугольник в 4 раз шире 
    [TestCase(20.0, 100.0/10)] // второй треугольник в 9 раз шире
    [TestCase(2.1, 95.238095238095241d)] // второй треугольник очень узкий
    // todo: добавть проверку что элементы не могут перекрывать друг друга -> тест тогда упадет
    // Мы считаем теплопроводность пластины, а тут получается, что пластина как бы загнута внутрь самой себя. Физически этого быть не может.
    // Проверка наверное должна быть такая, что ни один узел не может находиться внутри элемента - либо принадлежать ему, либо лежать снаружи.
    [TestCase(1.0, 66.666666666666671d)] // второй треугольник внутри первого
    [TestCase(0.0, 100.0/2)] // второй треугольник полностью накрывает первый
    public void MoveNodeD_AlongX_ReturnsExpectedResult(
        double xCoordinateNodeD, double expectedTemperature)
    {
        const double tempA = 0.0;
        const double tempD = 100.0;

        var solution = SolveWithNodeD(xCoordinateNodeD, tempA, tempD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));

            // Ребро BC: T(B) = T(C) = T_A + (T_D − T_A)·2/x_D.
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedTemperature).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedTemperature).Within(Tolerance));
        });
    }

    /// <summary>
    /// Линейная формула T(B) = T(C) = T_A + (T_D − T_A)·2/x_D верна
    /// для любых граничных значений — проверяем на разных парах.
    /// </summary>
    [TestCase(4.0, 0.0, 100.0)]
    [TestCase(8.0, 0.0, 100.0)]
    [TestCase(10.0, -50.0, 50.0)]
    [TestCase(5.0, 273.15, 373.15)]
    public void MoveNodeD_AlongX_FormulaWorks(
        double xCoordinateNodeD, double tempA, double tempD)
    {
        var solution = SolveWithNodeD(xCoordinateNodeD, tempA, tempD);
        const double xCoordinatesNodeBa = 2.0;

        var expectedTemperature = tempA + (tempD - tempA) * (xCoordinatesNodeBa / xCoordinateNodeD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedTemperature).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedTemperature).Within(Tolerance));
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Общие инварианты при сдвигах координат
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// При любом сдвиге B или D решение остаётся конечным
    /// и определено во всех 4 узлах.
    /// </summary>
    [Test]
    public void AnyCoordinateShift_SolutionRemainsFinite()
    {
        const double tempA = 20.0;
        const double tempD = 100.0;

        var shiftedB = SolveWithNodeB(0.5, tempA, tempD);
        var shiftedD = SolveWithNodeD(6.0, tempA, tempD);

        foreach (var solution in new[] { shiftedB, shiftedD })
        {
            Assert.That(solution.Items, Has.Count.EqualTo(4));
            foreach (var item in solution.Items)
            {
                Assert.That(double.IsFinite(item.Value), Is.True,
                    $"Узел '{item.Node.NodeId}' получил нефинитное значение.");
            }
        }
    }

    private static IFiniteElement BuildBaseElement1() =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(0.0, 1.0, "nodeA")
            .AddNode(2.0, 2.0, "nodeB")
            .AddNode(2.0, 0.0, "nodeC")
            .Build("The first triangle");

    private static IFiniteElement BuildBaseElement2(IFiniteElement element1) =>
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

    /// <summary>
    /// Модель с базовой геометрией, но узлом B, сдвинутым по Y.
    /// A(0,1), C(2,0), D(4,1) неподвижны.
    /// </summary>
    private static IConstructionModel BuildModelWithMovedNodeB(double yCoordinateNodeB)
    {
        var element1 = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(0.0, 1.0, "nodeA")
            .AddNode(2.0, yCoordinateNodeB, "nodeB")
            .AddNode(2.0, 0.0, "nodeC")
            .Build("The first triangle");

        var element2 = BuildBaseElement2(element1);

        return new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2)
            .Build();
    }

    /// <summary>
    /// Модель с базовой геометрией, но узлом D, сдвинутым по X.
    /// A(0,1), B(2,2), C(2,0) неподвижны.
    /// </summary>
    private static IConstructionModel BuildModelWithMovedNodeD(double xCoordinateNodeD)
    {
        var element1 = BuildBaseElement1();
        var element2 = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(element1.Node("nodeB"))
            .AddNode(element1.Node("nodeC"))
            .AddNode(xCoordinateNodeD, 1.0, "nodeD")
            .Build("The second triangle");

        return new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2)
            .Build();
    }

    private static ConstructionModelSolution SolveWithNodeB(
        double yCoordinate, double tempA, double tempD) =>
        new SolutionBuilder(BuildModelWithMovedNodeB(yCoordinate))
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

    private static ConstructionModelSolution SolveWithNodeD(
        double xCoordinate, double tempA, double tempD) =>
        new SolutionBuilder(BuildModelWithMovedNodeD(xCoordinate))
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

    private static double ValueOf(ConstructionModelSolution solution, string nodeId)
    {
        var item = solution.Items.SingleOrDefault(i => i.Node.NodeId == nodeId);
        Assert.That(item, Is.Not.Null, $"В решении отсутствует узел '{nodeId}'.");
        return item!.Value;
    }
}