using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.StiffnessMatrixLogic;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
public class Triangle2DLocalStiffnessMatrixLogicTests
{
    [Test]
    public void GetLocalMatrix_WhenSolverHasCorrectCoordinates1_ReturnsExpectedLocalMatrix()
    {
        // Arrange
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

        var triangleElementSolver = new Triangle2DLocalStiffnessMatrixLogic();

        // Act
        var matrix = triangleElementSolver.GetLocalMatrix(element, 1.0, 1.0);

        // Assert
        Assert.IsTrue(triangleElementSolver.ValidateMatrixIsSymmetric());
        Assert.IsTrue(triangleElementSolver.ValidateMatrixHasZeroRowSums());

        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            Assert.That(matrix[i, j],
                Is.EqualTo(expectedValues[i, j]));
    }

    [Test]
    public void GetLocalMatrix_WhenSolverHasCorrectCoordinates2_ReturnsExpectedLocalMatrix()
    {
        // Arrange
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

        var triangleElementSolver = new Triangle2DLocalStiffnessMatrixLogic();

        // Act
        var matrix = triangleElementSolver.GetLocalMatrix(element, 1.0, 1.0);

        // Assert
        Assert.IsTrue(triangleElementSolver.ValidateMatrixIsSymmetric());
        Assert.IsTrue(triangleElementSolver.ValidateMatrixHasZeroRowSums());

        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            Assert.That(matrix[i, j],
                Is.EqualTo(expectedValues[i, j]));
    }
}