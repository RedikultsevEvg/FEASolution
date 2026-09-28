using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.StiffnessMatrixLogic;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
[TestOf(typeof(StiffnessTriangle2DTemperatureLogic))]
public class StiffnessTriangle2DTemperatureLogicTests
{
    private const double Tolerance = 1e-9;
    private StiffnessTriangle2DTemperatureLogic _logic = null!;

    [SetUp]
    public void SetUp()
    {
        _logic = new StiffnessTriangle2DTemperatureLogic();
    }

    [Test]
    public void GetLocalMatrix_WhenElementHasNodesWithCorrectCoordinates1_ReturnsExpectedMatrix()
    {
        // Arrange
        const double thickness = 1.0;
        const double thermalConductivity = 1.0;

        double[,] expectedValues =
        {
            {  0.500, -0.250, -0.250 },
            { -0.250,  0.625, -0.375 },
            { -0.250, -0.375,  0.625 }
        };

        var element = new FiniteElementBuilder()
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0, 1.0)
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .Build("The test triangle");

        // Act
        var matrix = _logic.GetMatrix(element, thermalConductivity, thickness);

        // Assert
        AssertMatrixIsValid(_logic);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    [Test]
    public void GetLocalMatrix_WhenElementHasNodesWithCorrectCoordinates2_ReturnsExpectedMatrix()
    {
        // Arrange
        const double thickness = 1.0;
        const double thermalConductivity = 1.0;

        double[,] expectedValues =
        {
            {  0.625, -0.250, -0.375 },
            { -0.250,  0.500, -0.250 },
            { -0.375, -0.250,  0.625 }
        };

        var element = new FiniteElementBuilder()
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.0, 2.0)
            .AddNode(4.0, 1.0)
            .AddNode(2.0, 0)
            .Build("The test triangle");

        // Act
        var matrix = _logic.GetMatrix(element, thermalConductivity, thickness);

        // Assert
        AssertMatrixIsValid(_logic);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    // ---------- Helpers ----------

    private static void AssertMatrixIsValid(StiffnessTriangle2DTemperatureLogic logic)
    {
        Assert.Multiple(() =>
        {
            Assert.That(logic.ValidateMatrixIsSymmetric(), Is.True,
                "Stiffness matrix must be symmetric.");
            Assert.That(logic.ValidateMatrixHasZeroRowSums(), Is.True,
                "Stiffness matrix rows must sum to zero.");
        });
    }

    private static void AssertMatrixMatchesExpected(
        IFiniteElement element,
        StiffnessMatrix matrix,
        double[,] expected)
    {
        var nodes = element.Nodes.ToArray();

        for (int i = 0; i < nodes.Length; i++)
        for (int j = 0; j < nodes.Length; j++)
        {
            Assert.That(
                matrix[nodes[i], nodes[j]],
                Is.EqualTo(expected[i, j]).Within(Tolerance),
                $"Mismatch at row={i}, col={j}.");
        }
    }
}