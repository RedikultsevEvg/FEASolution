using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.IntegrationTests._2D;

/// <summary>
/// Интеграционные тесты по примеру <c>Model2DTwoTriangles</c>.
///
/// Упор на:
///   • точные значения вектора решения (аналитически вычислимые для линейных
///     треугольных КЭ при линейном распределении температуры по x);
///   • точный состав и порядок идентификаторов узлов в векторе решения;
///   • инвариантность к выбору значений и позиций граничных условий;
///   • инвариантность к физическим параметрам (k, толщина) — для задачи
///     Лапласа без источников решение от них не зависит.
///
/// Аналитическое обоснование.
/// ────────────────────────────
/// Два равных равнобедренных треугольника с вершинами:
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
/// Если граничные условия заданы в A и D как T_A, T_D, то при k = const,
/// t = const и отсутствии источников решение уравнения Лапласа на такой
/// (прямоугольной по x) области с линейной интерполяцией на треугольниках
/// даёт **линейный по x профиль**:
///   T(x) = T_A + (T_D − T_A) · x / 4.
///
/// Отсюда точные ожидаемые значения в узлах:
///   T(A) = T_A                     (x = 0)
///   T(B) = T_A + (T_D − T_A)·0.5   (x = 2)
///   T(C) = T_A + (T_D − T_A)·0.5   (x = 2)
///   T(D) = T_D                     (x = 4)
///
/// Это справедливо для ЛЮБЫХ пар значений (T_A, T_D) и не зависит от k и t.
/// Значения k и t влияют только на матрицу жёсткости и вектор нагрузки —
/// но не на решение краевой задачи Дирихле без источников.
/// </summary>
[TestFixture]
[Category("Integration")]
[TestOf(typeof(SolutionBuilder))]
public class TwoEqualTrianglesIntegrationTests
{
    private const double Tolerance = 1e-9;

    private static readonly string[] NodeIds = ["nodeA", "nodeB", "nodeC", "nodeD"];


    [TestCase(10.0, 50.0)]
    [TestCase(20.0, 100.0)]
    [TestCase(0.0, 0.0)]
    [TestCase(-50.0, 50.0)]
    [TestCase(100.0, -100.0)]
    [TestCase(1e6, -1e6)]
    [TestCase(273.15, 373.15)]
    public void ApplyToNodeANodeD_LinearProfile_SymmetricBoundaries(double tempA, double tempD)
    {
        var solution = Solve(tempA, tempD);

        var expectedB = tempA + (tempD - tempA) * 0.5;
        var expectedC = tempA + (tempD - tempA) * 0.5;

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA),
                "T(A) должно точно совпадать с граничным значением.");
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedB).Within(Tolerance),
                "T(B) должно лежать на линейном профиле между A и D.");
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedC).Within(Tolerance),
                "T(C) должно лежать на линейном профиле между A и D.");
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD),
                "T(D) должно точно совпадать с граничным значением.");
        });
    }

    /// <summary>
    /// Граничные условия на B и C — оба узла при x = 2.
    /// Решение вырождается: T(B) = T(C) = заданное, A и D свободны.
    /// Для связной задачи Дирихле с двумя закреплёнными узлами в одном
    /// вертикальном сечении линейный профиль по x вырождается в константу
    /// только при равенстве значений; при разных значениях система
    /// недоопределена (обе степени свободы «висят» без связи с границей).
    /// Проверяем случай равных значений — константа.
    /// </summary>
    [TestCase(42.0)]
    [TestCase(-42.0)]
    [TestCase(0.0)]
    public void ApplyToNodeBNodeC_EqualValues_SolutionIsConstant(double t)
    {
        var solution = SolveAt("nodeB", t, "nodeC", t);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(t));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(t));
            // Внутренние (по отношению к границе) узлы получают то же
            // постоянное значение — единственное решение уравнения Лапласа
            // с постоянной границей.
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(t).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(t).Within(Tolerance));
        });
    }

    /// <summary>
    /// Граничные условия на A и B. Ожидаем, что:
    ///   • T(A), T(B) точно равны заданным;
    ///   • остальные узлы конечны и лежат между min и max.
    /// Точную линейную формулу здесь не выводим — узлы A и B не на одной
    /// координате x, решение двумерно.
    /// </summary>
    [TestCase(0.0, 100.0, 77.777777777777757d, 88.888888888888886d)]
    [TestCase(-30.0, 70.0, 47.777777777777771d, 58.888888888888886d)]
    public void ApplyToNodeANodeB_SatisfyDirichletAndBounded(double tempA, double tB, double expectedC, double expectedB)
    {
        var solution = SolveAt("nodeA", tempA, "nodeB", tB);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(tB));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedC).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(expectedB).Within(Tolerance));
        });
    }

    /// <summary>
    /// Граничные условия на C и D.
    /// </summary>
    [TestCase(5.0, 95.0, 15.0d, 25.000000000000004d)]
    [TestCase(-1.0, 1.0, -0.77777777777777768d, -0.55555555555555558d)]
    public void ApplyToNodeCNodeD_SatisfyDirichletAndBounded(double tempC, double tempD, double expectedA, double expectedB)
    {
        var solution = SolveAt("nodeC", tempC, "nodeD", tempD);

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(tempC));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(expectedA).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedB).Within(Tolerance));
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Независимость от физических параметров (k и толщины)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Для уравнения Лапласа без источников решение не зависит от k и t —
    /// они входят как общий множитель в матрицу жёсткости и сокращаются.
    /// </summary>
    [TestCase(1.0, 1.0)]
    [TestCase(2.0, 1.0)]
    [TestCase(0.5, 1.0)]
    [TestCase(1.0, 2.0)]
    [TestCase(1.0, 0.5)]
    [TestCase(10.0, 10.0)]
    [TestCase(0.1, 0.1)]
    public void Solution_IsIndependent_OfConductivityAndThickness(double thermalConductivity, double thickness)
    {
        const double tempA = 20.0;
        const double tempD = 100.0;

        var solution = Solve(tempA, tempD, thermalConductivity, thickness);

        // Аналитическое решение — то же линейное по x, что и в базовом случае.
        var expectedB = tempA + (tempD - tempA) * 0.5;

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedB).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedB).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));
        });
    }

    /// <summary>
    /// Если значения (thermalConductivity) и (thickness) меняются но (thermalConductivity * thickness) не меняется,
    /// то решение остается тоже самое.
    /// </summary>
    [Test]
    public void Solution_IsSame_ForTheSameThermalConductivityMultyThickness()
    {
        const double tempA = 20.0;
        const double tempD = 100.0;

        var model1 = BuildModelWithDifferentTriangleMaterial(
            thermalConductivity1: 3.0, thickness1: 1.0,
            thermalConductivity2: 1.0, thickness2: 1.0);

        var model2 = BuildModelWithDifferentTriangleMaterial(
            thermalConductivity1: 1.0, thickness1: 3.0,
            thermalConductivity2: 0.5, thickness2: 2.0);

        var solution1 = new SolutionBuilder(model1)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

        var solution2 = new SolutionBuilder(model2)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

        Assert.Multiple(() =>
        {
            foreach (var nodeId in NodeIds)
            {
                Assert.That(ValueOf(solution2, nodeId),
                    Is.EqualTo(ValueOf(solution1, nodeId)).Within(Tolerance),
                    $"Значение в узле '{nodeId}' должно совпадать для однородных и разных k/t.");
            }
        });
    }

    /// <summary>
    /// При разных thermalConductivity у треугольников температура на общем
    /// ребре BC перестаёт быть серединой и определяется балансом потоков.
    ///
    /// Вывод формулы.
    /// ──────────────
    /// Область состоит из двух прямоугольных треугольников, у которых ребро BC
    /// лежит на прямой x = 2, а узлы A и D — на x = 0 и x = 4 соответственно.
    /// Решение Лапласа без источников в каждом треугольнике линейно по x,
    /// поэтому температура на BC постоянна: T(B) = T(C) = T_m.
    ///
    /// Поток через T1 (A → BC):  q₁ = k₁ · (T_m − T_A) / 2
    /// Поток через T2 (BC → D):  q₂ = k₂ · (T_D − T_m) / 2
    /// Из баланса q₁ = q₂:
    ///   k₁ · (T_m − T_A) = k₂ · (T_D − T_m)
    ///   T_m · (k₁ + k₂) = k₁·T_A + k₂·T_D
    ///   T_m = (k₁·T_A + k₂·T_D) / (k₁ + k₂)
    ///
    /// Формула не зависит от толщины (она входит множителем в оба потока
    /// и сокращается) и от абсолютных размеров по x, пока ребро BC делит
    /// расстояние A→D пополам.
    /// </summary>
    [TestCase(1.0, 1.0, 20.0, 100.0, 60.0)]                    // симметрия — середина
    [TestCase(2.0, 1.0, 20.0, 100.0, 140.0 / 3.0)]              // левый горячее
    [TestCase(1.0, 2.0, 20.0, 100.0, 220.0 / 3.0)]              // правый горячее
    [TestCase(10.0, 1.0, 20.0, 100.0, 300.0 / 11.0)]            // сильный контраст влево
    [TestCase(1.0, 10.0, 20.0, 100.0, 1020.0 / 11.0)]           // сильный контраст вправо
    [TestCase(3.0, 7.0, 20.0, 100.0, 76.0)]                     // несимметричные целые
    [TestCase(7.0, 3.0, 20.0, 100.0, 44.0)]                     // зеркально предыдущему
    [TestCase(0.5, 2.0, 20.0, 100.0, 84.0)]                     // дробные k
    [TestCase(4.0, 0.5, 20.0, 100.0, 130.0 / 4.5)]              // обратный контраст
    [TestCase(1.0, 1.0, 0.0, 0.0, 0.0)]                         // нулевые границы
    [TestCase(1.0, 1.0, -50.0, 50.0, 0.0)]                      // симметрично около нуля
    [TestCase(2.0, 3.0, 10.0, 40.0, (2.0 * 10.0 + 3.0 * 40.0) / 5.0)]  // произвольный набор
    public void DifferentThermalConductivity_ProducesExactEdgeTemperature(
        double thermalConductivity1, double thermalConductivity2, double tempA, double tempD, double expectedEdgeTemp)
    {
        var model = BuildModelWithDifferentTriangleMaterial(
            thermalConductivity1, thickness1: 1.0,
            thermalConductivity2, thickness2: 1.0);

        var solution = new SolutionBuilder(model)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));

            // Ребро BC: T(B) = T(C) = (k₁·T_A + k₂·T_D) / (k₁ + k₂).
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedEdgeTemp).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedEdgeTemp).Within(Tolerance));
        });
    }

    /// <summary>
    /// При разных толщинах у треугольников (при одинаковых thermalConductivity)
    /// температура на общем ребре BC определяется балансом потоков, а поток
    /// пропорционален произведению (k · t). При равных k это сводится к:
    ///
    ///   T_m = (t₁·T_A + t₂·T_D) / (t₁ + t₂)
    ///
    /// </summary>
    [TestCase(1.0, 1.0, 20.0, 100.0, 60.0)]                    // симметрия — середина
    [TestCase(2.0, 1.0, 20.0, 100.0, 140.0 / 3.0)]              // левый толще
    [TestCase(1.0, 2.0, 20.0, 100.0, 220.0 / 3.0)]              // правый толще
    [TestCase(10.0, 1.0, 20.0, 100.0, 300.0 / 11.0)]            // сильный контраст влево
    [TestCase(1.0, 10.0, 20.0, 100.0, 1020.0 / 11.0)]           // сильный контраст вправо
    [TestCase(3.0, 7.0, 20.0, 100.0, 76.0)]                     // несимметричные целые
    [TestCase(7.0, 3.0, 20.0, 100.0, 44.0)]                     // зеркально предыдущему
    [TestCase(0.5, 2.0, 20.0, 100.0, 84.0)]                     // дробные t
    [TestCase(4.0, 0.5, 20.0, 100.0, 130.0 / 4.5)]              // обратный контраст
    [TestCase(1.0, 1.0, 0.0, 0.0, 0.0)]                         // нулевые границы
    [TestCase(1.0, 1.0, -50.0, 50.0, 0.0)]                      // симметрично около нуля
    [TestCase(2.0, 3.0, 10.0, 40.0, (2.0 * 10.0 + 3.0 * 40.0) / 5.0)]  // произвольный набор
    public void DifferentThickness_ProducesExactEdgeTemperature(
        double thickness1, double thickness2, double tempA, double tempD, double expectedEdgeTemp)
    {
        var model = BuildModelWithDifferentTriangleMaterial(
            thermalConductivity1: 1.0, thickness1,
            thermalConductivity2: 1.0, thickness2);

        var solution = new SolutionBuilder(model)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(ValueOf(solution, "nodeA"), Is.EqualTo(tempA));
            Assert.That(ValueOf(solution, "nodeD"), Is.EqualTo(tempD));

            // Ребро BC: T(B) = T(C) = (t₁·T_A + t₂·T_D) / (t₁ + t₂).
            Assert.That(ValueOf(solution, "nodeB"), Is.EqualTo(expectedEdgeTemp).Within(Tolerance));
            Assert.That(ValueOf(solution, "nodeC"), Is.EqualTo(expectedEdgeTemp).Within(Tolerance));
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Идентификаторы узлов в векторе решения
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void SolutionVector_HasExactlyFourDistinctNodeIds()
    {
        var solution = Solve(20.0, 100.0);

        var ids = solution.Items.Select(i => i.Node.NodeId).ToList();

        Assert.That(ids, Has.Count.EqualTo(4),
            "В модели 4 уникальных узла; вектор решения не должен содержать дубликатов.");
        Assert.That(ids, Is.EquivalentTo(NodeIds));
    }

    [Test]
    public void SolutionVector_AllItems_HaveTemperatureFreedom()
    {
        var solution = Solve(20.0, 100.0);

        Assert.That(solution.Items, Is.Not.Empty);
        Assert.That(solution.Items.Select(i => i.Freedom).Distinct(),
            Is.EquivalentTo([Freedom.Temperature]));
    }

    /// <summary>
    /// Каждый <see cref="SolutionItem"/> ссылается ровно на тот же
    /// экземпляр <c>IElementNode</c>, что и исходная модель — это критично
    /// для сшивки узлов между элементами (nodeB, nodeC — общие).
    /// </summary>
    [Test]
    public void SolutionVector_NodeReferences_AreSharedBetweenElements()
    {
        var element1 = BuildElement1();
        var element2 = BuildElement2(element1);

        // Убеждаемся, что в исходных элементах nodeB и nodeC — те же ссылки.
        Assert.That(element2.Node("nodeB"), Is.SameAs(element1.Node("nodeB")),
            "nodeB должен быть общим между элементами.");
        Assert.That(element2.Node("nodeC"), Is.SameAs(element1.Node("nodeC")),
            "nodeC должен быть общим между элементами.");

        var model = new ConstructionModelBuilder()
            .AddElements(element1, element2)
            .Build();

        var solution = new SolutionBuilder(model)
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, 20.0)
            .AddDirichlet("nodeD", Freedom.Temperature, 100.0)
            .Build();

        var atB = solution.Items.Single(i => i.Node.NodeId == "nodeB");
        var atC = solution.Items.Single(i => i.Node.NodeId == "nodeC");

        Assert.That(atB.Node, Is.SameAs(element1.Node("nodeB")),
            "Узел nodeB в решении должен быть тем же экземпляром, что и в элементе.");
        Assert.That(atC.Node, Is.SameAs(element1.Node("nodeC")),
            "Узел nodeC в решении должен быть тем же экземпляром, что и в элементе.");
    }

    // ────────────────────────────────────────────────────────────────
    // Общие инварианты
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Solution_AllValues_AreFinite()
    {
        var solution = Solve(20.0, 100.0);

        Assert.That(solution.Items, Is.Not.Empty);
        foreach (var item in solution.Items)
        {
            Assert.That(double.IsFinite(item.Value), Is.True,
                $"Узел '{item.Node.NodeId}' получил нефинитное значение: {item.Value}.");
        }
    }

    [Test]
    public void Solution_IsDeterministic_AcrossRuns()
    {
        const double tempA = 20.0;
        const double tempD = 100.0;
        var first = Solve(tempA, tempD);
        var second = Solve(tempA, tempD);

        foreach (var nodeId in NodeIds)
        {
            Assert.That(ValueOf(second, nodeId),
                Is.EqualTo(ValueOf(first, nodeId)),
                $"Значение в узле '{nodeId}' нестабильно между запусками.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Негативные сценарии
    // ────────────────────────────────────────────────────────────────

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

        Assert.That(ex!.Message, Is.EqualTo("Construction model stiffness matrix is not assembled. Call 'Assembly' first."));
    }

    private static IFiniteElement BuildElement1(double thermalConductivity = 1.0, double thickness = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = thermalConductivity,
                Thickness = thickness,
            })
            .AddNode(0.0, 1.0, "nodeA")
            .AddNode(2.0, 2.0, "nodeB")
            .AddNode(2.0, 0.0, "nodeC")
            .Build("The first triangle");

    private static IFiniteElement BuildElement2(IFiniteElement element1, double k = 1.0, double t = 1.0) =>
        new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = k,
                Thickness = t,
            })
            .AddNode(element1.Node("nodeB"))
            .AddNode(element1.Node("nodeC"))
            .AddNode(4.0, 1.0, "nodeD")
            .Build("The second triangle");

    private static IConstructionModel BuildModel(double thermalConductivity = 1.0, double thickness = 1.0)
    {
        var element1 = BuildElement1(thermalConductivity, thickness);
        var element2 = BuildElement2(element1, thermalConductivity, thickness);

        return new ConstructionModelBuilder()
            .AddElements(element1, element2)
            .Build();
    }

    private static IConstructionModel BuildModelWithDifferentTriangleMaterial(
        double thermalConductivity1, double thickness1, double thermalConductivity2, double thickness2)
    {
        var element1 = BuildElement1(thermalConductivity1, thickness1);
        var element2 = BuildElement2(element1, thermalConductivity2, thickness2);

        return new ConstructionModelBuilder()
            .AddElements(element1, element2)
            .Build();
    }

    private static ConstructionModelSolution Solve(
        double tempA, double tempD, double thermalConductivity = 1.0, double thickness = 1.0) =>
        new SolutionBuilder(BuildModel(thermalConductivity, thickness))
            .Assembly()
            .AddDirichlet("nodeA", Freedom.Temperature, tempA)
            .AddDirichlet("nodeD", Freedom.Temperature, tempD)
            .Build();

    private static ConstructionModelSolution SolveAt(
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