using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Types;
using FeaSolution.MathNet.SleCalculation;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

[TestFixture]
[TestOf(typeof(InvertedMatrixSleCalculator))]
public class InvertedMatrixSleCalculatorTest : SleCalculatorTestBase
{
    private InvertedMatrixSleCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _calculator = new InvertedMatrixSleCalculator();
    }

    [Test]
    public void SolveSLE_WhenTheMatrixAndVectorIsCorrect_ReturnsExpectedValues()
    {
        // Arrange
        var expected = new[] { 20.0, 60.0, 60.0, 100.0 };

        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var n4 = MakeNodeMock("node4");

        var nodes = new[] { n1, n2, n3, n4 };

        // K_D :
        // [[1.000,  0.000,  0.000, 0.000],
        //  [0.000,  1.250, -0.750, 0.000],
        //  [0.000, -0.750,  1.250, 0.000],
        //  [0.000,  0.000,  0.000, 1.000]]
        var values = new (int indexI, int j, double v)[]
        {
            (0, 0,  1.000),
            (1, 1,  1.250),
            (1, 2, -0.750),
            (2, 2,  1.250),
            (3, 3,  1.000),
        };

        var matrixKWithDirichlet = MakeMatrixMock(4, nodes, values);

        // F_D = [20, 30, 30, 100]
        var vectorF = new[]
        {
            MakeItem(n1,  20.0),
            MakeItem(n2,  30.0),
            MakeItem(n3,  30.0),
            MakeItem(n4, 100.0),
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(result[i].Value, Is.EqualTo(expected[i]),
                $"Несовпадение температуры в узле {i + 1}");
        }
    }

    [Test]
    public void SolveSLE_WhenTheMatrixAndVectorIsCorrect_ResultItemsReferenceCorrectNodesAndFreedom()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var n4 = MakeNodeMock("node4");
        var nodes = new[] { n1, n2, n3, n4 };

        var matrixKWithDirichlet = MakeMatrixMock(4, nodes, [
            (0, 0, 1.0),
            (1, 1, 1.0),
            (2, 2, 1.0),
            (3, 3, 1.0)
        ]);

        var vectorF = new[]
        {
            MakeItem(n1, 10.0),
            MakeItem(n2, 20.0),
            MakeItem(n3, 30.0),
            MakeItem(n4, 40.0),
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Node, Is.SameAs(n1));
        Assert.That(result[1].Node, Is.SameAs(n2));
        Assert.That(result[2].Node, Is.SameAs(n3));
        Assert.That(result[3].Node, Is.SameAs(n4));

        Assert.That(result.Select(r => r.Freedom),
            Is.All.EqualTo(Freedom.Temperature));
    }

    [Test]
    public void SolveSLE_WhenDiagonalMatrix_ReturnsElementwiseDivision()
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
    public void SolveSLE_WhenIdentityMatrix_ReturnsRightHandSide()
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
    public void SolveSLE_WhenZeroSizeMatrix_ThrowsFeaCommonException()
    {
        // Arrange
        var matrixKWithDirichlet = MakeMatrixMock(0, [], []);

        var vectorF = Array.Empty<SolutionItem>();

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixKWithDirichlet, vectorF));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The size of source matrix is zero."));
    }

    [Test]
    public void SolveSLE_WhenSingularMatrix_ThrowsFeaCommonException()
    {
        // Arrange: 
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var n3 = MakeNodeMock("node3");
        var n4 = MakeNodeMock("node4");
        var nodes = new[] { n1, n2, n3, n4 };

        var singularMatrixK = MakeMatrixMock(4, nodes, [
            (0, 0,  0.500),
            (0, 1, -0.250),
            (0, 2, -0.250),
            (1, 1,  1.250),
            (1, 2, -0.750),
            (1, 3, -0.250),
            (2, 2,  1.250),
            (2, 3, -0.250),
            (3, 3,  0.500)
        ]);

        var zeroVectorF = new[]
        {
            MakeItem(n1, 0.0),
            MakeItem(n2, 0.0),
            MakeItem(n3, 0.0),
            MakeItem(n4, 0.0),
        };

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(singularMatrixK, zeroVectorF));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("At least one result value is NaN. The solution can't be calculated."));
    }

    [Test]
    public void SolveSLE_WhenCorrectMatrixAndVector_ResultOrderMatchesNodesOrder()
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

    [Test]
    public void SolveSLE__WhenCorrectMatrixAndVector_ReadsNonZeroValuesFromMatrix()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var nodes = new[] { n1, n2 };

        var matrixKWithDirichlet = MakeMatrixMock(2, nodes, [
            (0, 0, 2.0),
            (1, 1, 4.0)
        ]);

        var vectorF = new[]
        {
            MakeItem(n1, 2.0),
            MakeItem(n2, 8.0),
        };

        // Act
        _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        matrixKWithDirichlet.Received(1).NonZeroMatrixValuesWithIndexes();
        _ = matrixKWithDirichlet.Received(1).Nodes;
    }
}