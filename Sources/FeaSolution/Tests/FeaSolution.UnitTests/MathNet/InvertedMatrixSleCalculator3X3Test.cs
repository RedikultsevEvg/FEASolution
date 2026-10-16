using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.MathNet.SleCalculation;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

/// <summary>
/// Тесты для системы размерности 3×3 — нечётный размер,
/// невырожденная матрица с ненулевыми внедиагональными элементами.
/// </summary>
[TestFixture]
[TestOf(typeof(InvertedMatrixSleCalculator))]
public class InvertedMatrixSleCalculator3X3Test : SleCalculatorTestBase
{
    private InvertedMatrixSleCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _calculator = new InvertedMatrixSleCalculator();
    }

    /// <summary>
    /// Эталонная симметричная SPD-матрица 3×3, невырожденная.
    /// K = [[ 4, 1, 0],
    ///      [ 1, 3, 1],
    ///      [ 0, 1, 2]]
    /// </summary>
    private static (int i, int j, double v)[] ReferenceMatrix() =>
    [
        (0, 0, 4.0),
        (0, 1, 1.0),
        (1, 1, 3.0),
        (1, 2, 1.0),
        (2, 2, 2.0)
    ];

    [Test]
    public void SolveSLE_WhenSymmetric3x3Matrix_ReturnsExpectedSolution()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        // K = [[4, 1, 0],
        //      [1, 3, 1],
        //      [0, 1, 2]]
        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, ReferenceMatrix());

        // F = [1, 2, 3]
        // Решение:
        //   4x +  y     = 1
        //    x + 3y + z = 2
        //        y + 2z = 3
        // Решаем: из 1-го y = 1 - 4x; подставляем во 2-е: x + 3(1-4x) + z = 2 → z = 11x - 1;
        // из 3-го: (1-4x) + 2(11x-1) = 3 → 1 - 4x + 22x - 2 = 3 → 18x = 4 → x = 2/9;
        // y = 1 - 8/9 = 1/9; z = 11·(2/9) - 1 = 22/9 - 9/9 = 13/9.
        var vectorF = new[]
        {
            MakeItem(n1, 1.0),
            MakeItem(n2, 2.0),
            MakeItem(n3, 3.0),
        };

        var expected = new[] { 2.0 / 9.0, 1.0 / 9.0, 13.0 / 9.0 };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(3));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(result[i].Value, Is.EqualTo(expected[i]).Within(Tolerance),
                $"Несовпадение решения в узле {i + 1}");
        }
    }

    [Test]
    public void SolveSLE_WhenSymmetric3x3Matrix_ResultItemsReferenceCorrectNodesAndFreedom()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, ReferenceMatrix());
        var vectorF = new[]
        {
            MakeItem(n1, 1.0),
            MakeItem(n2, 2.0),
            MakeItem(n3, 3.0),
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Node, Is.SameAs(n1));
        Assert.That(result[1].Node, Is.SameAs(n2));
        Assert.That(result[2].Node, Is.SameAs(n3));

        Assert.That(result.Select(r => r.Freedom),
            Is.All.EqualTo(Freedom.Temperature));
    }

    [Test]
    public void SolveSLE_When3x3DiagonalMatrix_ReturnsElementwiseDivision()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, [
            (0, 0, 2.0),
            (1, 1, 4.0),
            (2, 2, 5.0)
        ]);

        var vectorF = new[]
        {
            MakeItem(n1,  6.0),   // 6 / 2 = 3
            MakeItem(n2,  8.0),   // 8 / 4 = 2
            MakeItem(n3, 10.0),   // 10 / 5 = 2
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Value, Is.EqualTo(3.0));
        Assert.That(result[1].Value, Is.EqualTo(2.0));
        Assert.That(result[2].Value, Is.EqualTo(2.0));
    }

    [Test]
    public void SolveSLE_When3x3IdentityMatrix_ReturnsRightHandSide()
    {
        // Arrange
        var nodes = new[]
        {
            MakeNodeMock("node1"),
            MakeNodeMock("node2"),
            MakeNodeMock("node3")
        };
        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, [
            (0, 0, 1.0),
            (1, 1, 1.0),
            (2, 2, 1.0)
        ]);
        var expectedVectorT = new[] { 10.0, 20.0, 30.0 };

        var vectorF = nodes
            .Select((node, index) => MakeItem(node, expectedVectorT[index]))
            .ToArray();

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        for (var i = 0; i < expectedVectorT.Length; i++)
            Assert.That(result[i].Value, Is.EqualTo(expectedVectorT[i]));
    }

    [Test]
    public void SolveSLE_When3x3MatrixWithDirichletPattern_ReturnsExpectedSolution()
    {
        // Arrange: имитация системы после учёта условий Дирихле —
        // первая и третья строки/столбцы единичные, средняя — обычная.
        // K_D = [[1,    0,    0  ],
        //        [0,    1.25, -0.75],
        //        [0,    -0.75,    1  ]]
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, [
            (0, 0,  1.00),
            (1, 1,  1.25),
            (1, 2, -0.75),
            (2, 2,  1.00)
        ]);

        // F = [20, 30, 100]
        // Уравнение для узла 2: 1.25·T2 - 0.75·T3 = 30, при T3 = 100 → 1.25·T2 = 105 → T2 = 84
        var vectorF = new[]
        {
            MakeItem(n1,  20.0),
            MakeItem(n2,  30.0),
            MakeItem(n3, 100.0),
        };

        var expected = new[] { 20.0, 1680.0 / 11.0, 2360.0 / 11.0 };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (int i = 0; i < expected.Length; i++)
                Assert.That(result[i].Value, Is.EqualTo(expected[i]).Within(1e-12),
                    $"Несовпадение решения в узле {i + 1}");
        });
    }

    [Test]
    public void SolveSLE_When3x3SingularMatrix_ThrowsFeaCommonException()
    {
        // Arrange: матрица с линейно зависимыми строками → сингулярна.
        // K = [[1, 2, 3],
        //      [2, 4, 6],  // = 2 × первая
        //      [1, 1, 1]]
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var singularMatrixK = MakeMatrixMock(3, nodes, [
            (0, 0, 1.0),
            (0, 1, 2.0),
            (0, 2, 3.0),
            (1, 1, 4.0),
            (1, 2, 6.0),
            (2, 2, 1.0)
        ]);

        var vectorF = new[]
        {
            MakeItem(n1, 1.0),
            MakeItem(n2, 2.0),
            MakeItem(n3, 3.0),
        };

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(singularMatrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "At least one result value is NaN. The solution can't be calculated."));
    }

    [Test]
    public void SolveSLE_When3x3MatrixWithZeroRhs_ReturnsZeroVector()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, ReferenceMatrix());

        var vectorF = new[]
        {
            MakeItem(n1, 0.0),
            MakeItem(n2, 0.0),
            MakeItem(n3, 0.0),
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result.All(r => r.Value == 0.0), Is.True);
    }

    [Test]
    public void SolveSLE_When3x3Matrix_ReadsNonZeroValuesFromMatrixOnce()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var nodes = new[] { n1, n2, n3 };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, ReferenceMatrix());

        var vectorF = new[]
        {
            MakeItem(n1, 1.0),
            MakeItem(n2, 1.0),
            MakeItem(n3, 1.0),
        };

        // Act
        _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        matrixKWithDirichlet.Received(1).NonZeroMatrixValuesWithIndexes();
        _ = matrixKWithDirichlet.Received(1).Nodes;
    }

    [Test]
    public void SolveSLE_When3x3Matrix_ResultOrderMatchesNodesOrder()
    {
        // Arrange
        var a = MakeNodeMock("100");
        var b = MakeNodeMock("200");
        var c = MakeNodeMock("300");
        var nodes = new[] { a, b, c };

        var matrixKWithDirichlet = MakeMatrixMock(3, nodes, [
            (0, 0, 1.0),
            (1, 1, 1.0),
            (2, 2, 1.0)
        ]);

        var vectorF = nodes.Select(n => MakeItem(n, 1.0)).ToArray();

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Node, Is.SameAs(a));
        Assert.That(result[1].Node, Is.SameAs(b));
        Assert.That(result[2].Node, Is.SameAs(c));
    }
}