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
    public void GetLocalMatrix_WhenThermalConductivityMoreThanThickness_ReturnsExpectedMatrix()
    {
        // Arrange
        const double thickness = 1.0;
        const double thermalConductivity = 4.0;

        double[,] expectedValues =
        {
            {  2.000, -1.000, -1.000 },
            { -1.000,  2.500, -1.500 },
            { -1.000, -1.500,  2.500 }
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
        AssertMatrixIsValid(matrix);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    [Test]
    public void GetLocalMatrix_WhenThicknessIsMoreThanThermalConductivity_ReturnsExpectedMatrix()
    {
        // Arrange
        const double thickness = 2.0;
        const double thermalConductivity = 1.0;

        double[,] expectedValues =
        {
            {  1.250, -0.500, -0.750 },
            { -0.500,  1.000, -0.500 },
            { -0.750, -0.500,  1.250 }
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
        AssertMatrixIsValid(matrix);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    // ---------- Helpers ----------

    private static void AssertMatrixIsValid(StiffnessMatrix matrix)
    {
        Assert.Multiple(() =>
        {
            Assert.That(matrix.ValidateMatrixIsSymmetric(), Is.True,
                "Stiffness matrix must be symmetric.");
            Assert.That(matrix.ValidateMatrixHasZeroRowSums(), Is.True,
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