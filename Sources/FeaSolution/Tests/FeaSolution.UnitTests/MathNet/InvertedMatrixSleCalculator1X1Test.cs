using FeaSolution.Core.Exceptions;
using FeaSolution.MathNet.SleCalculation;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

/// <summary>
/// Тесты для системы размерности 1×1 — граничный случай.
/// </summary>
[TestFixture]
[TestOf(typeof(InvertedMatrixSleCalculator))]
public class InvertedMatrixSleCalculator1X1Test : SleCalculatorTestBase
{
    private InvertedMatrixSleCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _calculator = new InvertedMatrixSleCalculator();
    }

    [Test]
    public void SolveSLE_WhenScalarMatrixWithDirichlet_ReturnsExpectedValue()
    {
        // Arrange
        var expected = 3.0;

        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        // K_D = [[2.0]]
        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, 2.0)
        ]);

        // F_D = [6.0]  →  T = 6 / 2 = 3
        var vectorF = new[]
        {
            MakeItem(n1, 6.0)
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].Value, Is.EqualTo(expected));
    }

    [Test]
    public void SolveSLE_WhenScalarIdentityMatrix_ReturnsRightHandSide()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, 1.0)
        ]);

        var vectorF = new[]
        {
            MakeItem(n1, 42.0)
        };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Value, Is.EqualTo(42.0));
    }

    [Test]
    public void SolveSLE_WhenScalarMatrix_ResultItemReferencesCorrectNodeAndFreedom()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, 1.0)
        ]);

        var vectorF = new[] { MakeItem(n1, 10.0) };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Node, Is.SameAs(n1));
        Assert.That(result[0].Freedom, Is.EqualTo(Core.Enums.Freedom.Temperature));
    }

    [Test]
    public void SolveSLE_WhenScalarMatrixWithZeroDiagonal_ThrowsFeaCommonException()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        // K = [[0.0]] — вырожденная
        var singularMatrixK = MakeMatrixMock(1, nodes, [
            (0, 0, 0.0)
        ]);

        var vectorF = new[] { MakeItem(n1, 0.0) };

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(singularMatrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "At least one result value is NaN. The solution can't be calculated."));
    }

    [Test]
    public void SolveSLE_WhenScalarMatrixWithNegativeDiagonal_ReturnsNegativeValue()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, -2.0)
        ]);

        // F = 6 → T = 6 / (-2) = -3
        var vectorF = new[] { MakeItem(n1, 6.0) };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Value, Is.EqualTo(-3.0));
    }

    [Test]
    public void SolveSLE_WhenScalarMatrixWithZeroRhs_ReturnsZero()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, 5.0)
        ]);

        var vectorF = new[] { MakeItem(n1, 0.0) };

        // Act
        var result = _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        Assert.That(result[0].Value, Is.EqualTo(0.0));
    }

    [Test]
    public void SolveSLE_WhenScalarMatrix_ReadsNonZeroValuesFromMatrixOnce()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var nodes = new[] { n1 };

        var matrixKWithDirichlet = MakeMatrixMock(1, nodes, [
            (0, 0, 1.0)
        ]);

        var vectorF = new[] { MakeItem(n1, 1.0) };

        // Act
        _calculator.SolveSLE(matrixKWithDirichlet, vectorF);

        // Assert
        matrixKWithDirichlet.Received(1).NonZeroMatrixValuesWithIndexes();
        _ = matrixKWithDirichlet.Received(1).Nodes;
    }
}