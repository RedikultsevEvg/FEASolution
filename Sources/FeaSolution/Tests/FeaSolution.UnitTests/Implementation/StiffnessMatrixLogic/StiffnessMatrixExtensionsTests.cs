using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using Moq;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
[TestOf(typeof(StiffnessMatrixExtensions))]
public class StiffnessMatrixExtensionsTests
{
    private const double Tolerance = 1e-9;

    [Test]
    public void AddMatrix_TwoTriangles_ProducesGlobalMatrix()
    {
        // Arrange
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();
        var node3 = Mock.Of<IElementNode>();
        var node4 = Mock.Of<IElementNode>();

        var nodes = new[] { node1, node2, node3, node4 };

        // Локальная матрица E1 (соответствует узлам 1, 2, 3).
        var k1 = new SmallStiffnessMatrix([node1, node2, node3])
        {
            [node1, node1] = 0.500,
            [node1, node2] = -0.250,
            [node1, node3] = -0.250,
            [node2, node2] = 0.625,
            [node2, node3] = -0.375,
            [node3, node3] = 0.625,
        };

        // Локальная матрица E2 (соответствует узлам 2, 4, 3).
        var k2 = new SmallStiffnessMatrix([node2, node4, node3])
        {
            [node2, node2] = 0.625,
            [node2, node4] = -0.250,
            [node2, node3] = -0.375,
            [node4, node4] = 0.500,
            [node4, node3] = -0.250,
            [node3, node3] = 0.625,
        };

        // Ожидаемая глобальная матрица из раздела 6 отчёта.
        double[,] expected =
        {
            {  0.500, -0.250, -0.250,  0.000 },
            { -0.250,  1.250, -0.750, -0.250 },
            { -0.250, -0.750,  1.250, -0.250 },
            {  0.000, -0.250, -0.250,  0.500 },
        };

        var global = new StiffnessMatrix();

        // Act
        global.AddMatrix(k1);
        global.AddMatrix(k2);

        // Assert
        Assert.That(global.NodeCount, Is.EqualTo(4),
            "Все четыре узла должны быть зарегистрированы.");

        Assert.Multiple(() =>
        {
            for (int i = 0; i < nodes.Length; i++)
                for (int j = 0; j < nodes.Length; j++)
                {
                    Assert.That(
                        global[nodes[i], nodes[j]],
                        Is.EqualTo(expected[i, j]).Within(Tolerance),
                        $"Mismatch at global row={i}, col={j}.");
                }
        });
    }

    [Test]
    public void AddMatrix_ResultIsSymmetric()
    {
        // Arrange
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();
        var node3 = Mock.Of<IElementNode>();
        var node4 = Mock.Of<IElementNode>();

        var nodes = new[] { node1, node2, node3, node4 };

        var k1 = new SmallStiffnessMatrix([node1, node2, node3])
        {
            [node1, node1] = 0.500,
            [node1, node2] = -0.250,
            [node1, node3] = -0.250,
            [node2, node2] = 0.625,
            [node2, node3] = -0.375,
            [node3, node3] = 0.625,
        };

        var k2 = new SmallStiffnessMatrix([node4, node3, node2])
        {
            [node2, node2] = 0.625,
            [node2, node4] = -0.250,
            [node2, node3] = -0.375,
            [node4, node4] = 0.500,
            [node4, node3] = -0.250,
            [node3, node3] = 0.625,
        };

        var global = new StiffnessMatrix();

        // Act
        global.AddMatrix(k1);
        global.AddMatrix(k2);

        // Assert — проверка симметрии через полный обход
        Assert.Multiple(() =>
        {
            for (int i = 0; i < nodes.Length; i++)
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    Assert.That(
                        global[nodes[i], nodes[j]],
                        Is.EqualTo(global[nodes[j], nodes[i]]).Within(Tolerance),
                        $"Asymmetry at ({i}, {j}).");
                }
        });
    }

    [Test]
    public void AddMatrix_RowSumsAreZero()
    {
        // Arrange
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();
        var node3 = Mock.Of<IElementNode>();
        var node4 = Mock.Of<IElementNode>();

        var nodes = new[] { node1, node2, node3, node4 };

        var k1 = new SmallStiffnessMatrix([node1, node2, node3])
        {
            [node1, node1] = 0.500,
            [node1, node2] = -0.250,
            [node1, node3] = -0.250,
            [node2, node2] = 0.625,
            [node2, node3] = -0.375,
            [node3, node3] = 0.625,
        };

        var k2 = new SmallStiffnessMatrix([node2, node3, node4])
        {
            [node2, node2] = 0.625,
            [node2, node4] = -0.250,
            [node2, node3] = -0.375,
            [node4, node4] = 0.500,
            [node4, node3] = -0.250,
            [node3, node3] = 0.625,
        };

        var global = new StiffnessMatrix();

        // Act
        global.AddMatrix(k1);
        global.AddMatrix(k2);

        // Assert — сумма каждой строки должна быть нулевой
        Assert.Multiple(() =>
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                double rowSum = 0.0;
                for (int j = 0; j < nodes.Length; j++)
                    rowSum += global[nodes[i], nodes[j]];

                Assert.That(rowSum, Is.EqualTo(0.0).Within(Tolerance),
                    $"Row {i} sum is not zero.");
            }
        });
    }
}