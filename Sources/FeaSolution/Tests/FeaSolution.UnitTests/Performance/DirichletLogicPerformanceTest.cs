using System.Diagnostics;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.DirichletCalculation;
using FeaSolution.Implementation.StiffnessMatrixs;
using NSubstitute;

namespace FeaSolution.UnitTests.Performance;

[TestFixture]
[TestOf(typeof(DirichletLogic))]
[Category("Performance")]
public class DirichletLogicPerformanceTest
{
    private DirichletLogic _dirichletLogic = null!;

    [SetUp]
    public void SetUp()
    {
        _dirichletLogic = new DirichletLogic();
    }

    // =====================================================================
    // 1. Абсолютное время на матрице разумного размера
    // =====================================================================

    [TestCase(100, 1)]
    [TestCase(1_000, 1)]
    [TestCase(10_000, 10)]
    public void ApplyDirichlet_TimeForSize(int n, int expectedMaxMiliSeconds)
    {
        // Arrange
        WarmUp(100);

        var (k, nodes) = CreateChainMatrix(n);
        var f = CreateVectorF(nodes);
        var conditions = CreateBoundaryConditions(nodes);

        // Act
        var sw = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k, f, conditions);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"n = {n}, expected max = {expectedMaxMiliSeconds} ms, " +
            $"actual = {sw.Elapsed.TotalMilliseconds:F3} ms");

        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
            $"ApplyDirichlet for n={n} took too long: " +
            $"{sw.Elapsed.TotalMilliseconds:F3} ms (max {expectedMaxMiliSeconds} ms).");
    }

    // =====================================================================
    // 2. Масштабирование: время должно расти линейно
    // =====================================================================

    [Test]
    public void ApplyDirichlet_ScalesLinearlyWithSize()
    {
        // Arrange
        WarmUp(1_000);

        var n1 = 1_000;
        var n2 = 10_000;

        // Act — замер для n1
        var (k1, nodes1) = CreateChainMatrix(n1);
        var f1 = CreateVectorF(nodes1);
        var conditions1 = CreateBoundaryConditions(nodes1);

        var sw1 = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k1, f1, conditions1);
        sw1.Stop();

        // Act — замер для n2
        var (k2, nodes2) = CreateChainMatrix(n2);
        var f2 = CreateVectorF(nodes2);
        var conditions2 = CreateBoundaryConditions(nodes2);

        var sw2 = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k2, f2, conditions2);
        sw2.Stop();

        // Assert
        var ratio = (double)sw2.ElapsedTicks / Math.Max(sw1.ElapsedTicks, 1);
        TestContext.WriteLine(
            $"n1={n1}: {sw1.Elapsed.TotalMilliseconds:F3} ms, " +
            $"n2={n2}: {sw2.Elapsed.TotalMilliseconds:F3} ms, " +
            $"ratio={ratio:F2}");

        // При линейном масштабировании ratio ≈ 10.
        // Допускаем широкий диапазон из-за шума и overhead'ов.
        Assert.That(ratio, Is.LessThan(100),
            $"Scaling looks super-linear: ratio={ratio:F2} (expected ~10 for 10x size).");
    }

    // =====================================================================
    // 3. Аллокации: метод не должен аллоцировать много
    // =====================================================================

    [Test]
    public void ApplyDirichlet_AllocationsAreBounded()
    {
        // Arrange
        const int n = 1_000;
        WarmUp(n);

        var (k, nodes) = CreateChainMatrix(n);
        var f = CreateVectorF(nodes);
        var conditions = CreateBoundaryConditions(nodes);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        _dirichletLogic.ApplyDirichlet(k, f, conditions);
        var after = GC.GetAllocatedBytesForCurrentThread();
        var allocated = after - before;

        // Assert
        TestContext.WriteLine($"Allocated for n={n}: {allocated} bytes");

        // Реализация не должна аллоцировать пропорционально n.
        // Допускаем небольшой overhead (итератор foreach по ICollection и т.п.).
        Assert.That(allocated, Is.LessThan(10_000),
            $"ApplyDirichlet allocated too much: {allocated} bytes for n={n}.");
    }

    // =====================================================================
    // 4. Многократные вызовы: нет утечек и деградации
    // =====================================================================

    [Test]
    public void ApplyDirichlet_ManyInvocations_StableTime()
    {
        // Arrange
        const int n = 1_000;
        const int iterations = 100;
        WarmUp(n);

        // Act
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var (k, nodes) = CreateChainMatrix(n);
            var f = CreateVectorF(nodes);
            var conditions = CreateBoundaryConditions(nodes);
            _dirichletLogic.ApplyDirichlet(k, f, conditions);
        }
        sw.Stop();

        // Assert
        var perCall = sw.Elapsed.TotalMilliseconds / iterations;
        TestContext.WriteLine(
            $"{iterations} invocations of n={n}: " +
            $"total={sw.Elapsed.TotalMilliseconds:F1} ms, per call={perCall:F3} ms");

        Assert.That(perCall, Is.LessThan(50),
            $"Per-call time degraded: {perCall:F3} ms per invocation.");
    }

    // =====================================================================
    // 5. Число условий: влияние на время
    // =====================================================================

    [TestCase(2, 1)]
    [TestCase(10, 4)]
    [TestCase(100, 30)]
    public void ApplyDirichlet_ManyConditions_TimeGrowsLinearly(
        int conditionCount, int expectedMaxMiliSeconds)
    {
        // Arrange
        const int n = 1_000;
        WarmUp(n, iterations: 1);

        var (k, nodes) = CreateChainMatrix(n);
        var f = CreateVectorF(nodes);

        var conditions = new List<DirichletCondition>(conditionCount);
        for (var i = 0; i < conditionCount; i++)
        {
            var index = i * (n / conditionCount);
            conditions.Add(new DirichletCondition
            {
                Node = nodes[index],
                Freedom = Freedom.Temperature,
                Value = 20.0 + i
            });
        }

        // Act
        var sw = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k, f, conditions);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"n={n}, conditions={conditionCount}, " +
            $"expected max={expectedMaxMiliSeconds} ms, " +
            $"actual={sw.Elapsed.TotalMilliseconds:F3} ms");

        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(expectedMaxMiliSeconds),
            $"ApplyDirichlet with {conditionCount} conditions took too long: " +
            $"{sw.Elapsed.TotalMilliseconds:F3} ms (max {expectedMaxMiliSeconds} ms).");
    }

    // =====================================================================
    // 6. Худший случай: условие на каждый узел
    // =====================================================================

    [Test]
    public void ApplyDirichlet_AllNodesConstrained_WorstCase()
    {
        // Arrange
        const int n = 1_000;
        WarmUp(n, iterations: 1);

        var (k, nodes) = CreateChainMatrix(n);
        var f = CreateVectorF(nodes);

        var conditions = new List<DirichletCondition>(n);
        for (var i = 0; i < n; i++)
        {
            conditions.Add(new DirichletCondition
            {
                Node = nodes[i],
                Freedom = Freedom.Temperature,
                Value = 20.0 + i
            });
        }

        // Act
        var sw = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k, f, conditions);
        sw.Stop();

        // Assert
        TestContext.WriteLine(
            $"n={n}, all nodes constrained: {sw.Elapsed.TotalMilliseconds:F3} ms");

        // O(n²) из-за FindNodeIndex внутри каждого ApplyDirichletOnce.
        // При n=1000 это ~10^6 операций — должно уложиться в разумное время.
        Assert.That(sw.Elapsed.TotalMilliseconds, Is.LessThan(5_000),
            $"Worst case took too long: {sw.Elapsed.TotalMilliseconds:F3} ms");
    }

    // =====================================================================
    // 7. Сравнение порядка условий
    // =====================================================================

    [Test]
    public void ApplyDirichlet_ConditionOrder_TimeDifferenceIsNegligible()
    {
        // Arrange
        const int n = 1_000;
        WarmUp(n);

        var conditionsFirst = new List<DirichletCondition>();
        var conditionsSecond = new List<DirichletCondition>();

        {
            var (_, nodes) = CreateChainMatrix(n);
            conditionsFirst.Add(new DirichletCondition { Node = nodes[0], Freedom = Freedom.Temperature, Value = 20.0 });
            conditionsFirst.Add(new DirichletCondition { Node = nodes[n / 2], Freedom = Freedom.Temperature, Value = 50.0 });
            conditionsFirst.Add(new DirichletCondition { Node = nodes[^1], Freedom = Freedom.Temperature, Value = 100.0 });
            conditionsSecond.AddRange(conditionsFirst.AsEnumerable().Reverse());
        }

        // Act — порядок «как есть»
        var (k1, nodes1) = CreateChainMatrix(n);
        var f1 = CreateVectorF(nodes1);
        var firstConditions = new List<DirichletCondition>
        {
            new() { Node = nodes1[0], Freedom = Freedom.Temperature, Value = 20.0 },
            new() { Node = nodes1[n / 2], Freedom = Freedom.Temperature, Value = 50.0 },
            new() { Node = nodes1[^1], Freedom = Freedom.Temperature, Value = 100.0 }
        };

        var sw1 = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k1, f1, firstConditions);
        sw1.Stop();

        // Act — обратный порядок
        var (k2, nodes2) = CreateChainMatrix(n);
        var f2 = CreateVectorF(nodes2);
        var secondConditions = new List<DirichletCondition>
        {
            new() { Node = nodes2[^1], Freedom = Freedom.Temperature, Value = 100.0 },
            new() { Node = nodes2[n / 2], Freedom = Freedom.Temperature, Value = 50.0 },
            new() { Node = nodes2[0], Freedom = Freedom.Temperature, Value = 20.0 }
        };

        var sw2 = Stopwatch.StartNew();
        _dirichletLogic.ApplyDirichlet(k2, f2, secondConditions);
        sw2.Stop();

        // Assert
        TestContext.WriteLine(
            $"Forward: {sw1.Elapsed.TotalMilliseconds:F3} ms, " +
            $"Reverse: {sw2.Elapsed.TotalMilliseconds:F3} ms");

        var max = Math.Max(sw1.ElapsedTicks, sw2.ElapsedTicks);
        var min = Math.Max(Math.Min(sw1.ElapsedTicks, sw2.ElapsedTicks), 1);
        var ratio = (double)max / min;

        Assert.That(ratio, Is.LessThan(10),
            $"Order of conditions affects time too much: ratio={ratio:F2}");
    }


    // =====================================================================
    // Вспомогательные методы
    // =====================================================================

    /// <summary>
    /// Создаёт «цепочку» узлов и диагональную с внедиагональными связями матрицу
    /// размера n×n. Матрица — трёхдиагональная, как типичная 1D-задача.
    /// </summary>
    private static (StiffnessMatrix matrix, IElementNode[] nodes) CreateChainMatrix(int n)
    {
        var nodes = new IElementNode[n];
        for (var i = 0; i < n; i++)
        {
            var node = Substitute.For<IElementNode>();
            node.NodeId.Returns($"node{i}");
            nodes[i] = node;
        }

        var k = new StiffnessMatrix();

        // Диагональ
        for (var i = 0; i < n; i++)
        {
            k.AddValue(nodes[i], nodes[i], 2.0);
        }

        // Внедиагональные — только соседние
        for (var i = 0; i < n - 1; i++)
        {
            k.AddValue(nodes[i], nodes[i + 1], -1.0);
        }

        return (k, nodes);
    }

    /// <summary>
    /// Создаёт вектор F нулевых SolutionItem по узлам.
    /// </summary>
    private static SolutionItem[] CreateVectorF(IElementNode[] nodes)
    {
        var f = new SolutionItem[nodes.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            f[i] = new SolutionItem
            {
                Node = nodes[i],
                Freedom = Freedom.Temperature,
                Value = 0.0
            };
        }
        return f;
    }

    /// <summary>
    /// Создаёт условия Дирихле на «краях» цепочки.
    /// </summary>
    private static List<DirichletCondition> CreateBoundaryConditions(IElementNode[] nodes)
        =>
        [
            new() { Node = nodes[0], Freedom = Freedom.Temperature, Value = 20.0 },
            new() { Node = nodes[^1], Freedom = Freedom.Temperature, Value = 100.0 }
        ];

    /// <summary>
    /// Прогревает JIT: выполняет операцию несколько раз, чтобы исключить
    /// влияние компиляции на измерения.
    /// </summary>
    private void WarmUp(int n, int iterations = 3)
    {
        for (var i = 0; i < iterations; i++)
        {
            var (k, nodes) = CreateChainMatrix(n);
            var f = CreateVectorF(nodes);
            var conditions = CreateBoundaryConditions(nodes);
            _dirichletLogic.ApplyDirichlet(k, f, conditions);
        }
    }
}