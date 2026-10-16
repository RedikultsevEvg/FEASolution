using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using FeaSolution.Implementation.StiffnessMatrixs;
using NSubstitute;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
[TestOf(typeof(StiffnessMatrixTriangle2DTemperatureLogic))]
public class StiffnessMatrixTriangle2DTemperatureLogicTests
{
    private const double Tolerance = 1e-9;
    private StiffnessMatrixTriangle2DTemperatureLogic _logic = null!;

    [SetUp]
    public void SetUp()
    {
        _logic = new StiffnessMatrixTriangle2DTemperatureLogic();
    }

    [Test]
    public void GetMatrix_WhenThermalConductivityMoreThanThickness_ReturnsExpectedMatrix()
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
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = thickness,
                ThermalConductivity = thermalConductivity
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0, 1.0)
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .Build("The test triangle");

        // Act
        var matrix = _logic.GetMatrix(element);

        // Assert
        AssertMatrixIsValid(matrix);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    [Test]
    public void GetMatrix_WhenThicknessIsMoreThanThermalConductivity_ReturnsExpectedMatrix()
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
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = thickness,
                ThermalConductivity = thermalConductivity
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.0, 2.0)
            .AddNode(4.0, 1.0)
            .AddNode(2.0, 0)
            .Build("The test triangle");

        // Act
        var matrix = _logic.GetMatrix(element);

        // Assert
        AssertMatrixIsValid(matrix);
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    // ---------------------------------------------------------------------
    // Guard clauses
    // ---------------------------------------------------------------------

    [Test]
    public void GetMatrix_WhenNodeTypeIsNotTwoDimensional_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(0.0)
            .AddNode(1.0)
            .AddNode(2.0)
            .Build("1D element");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void GetMatrix_WhenNodesCountIsNotThree_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(1.0, 1.0)
            .AddNode(0.0, 1.0)
            .Build("Quad element");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void GetMatrix_WhenFreedomsCountIsNotOne_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .AddFreedom(Freedom.AnotherFreedom, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Two freedoms");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void GetMatrix_WhenFreedomIsNotTemperature_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.AnotherFreedom, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Wrong freedom");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void GetMatrix_WhenOptionsAreNotTemperatureOptions_Throws()
    {
        // Arrange
        var wrongOptions = Substitute.For<IElementOptions>();
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, wrongOptions)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Wrong options");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    // ---------------------------------------------------------------------
    // Degenerate geometry
    // ---------------------------------------------------------------------

    [Test]
    public void GetMatrix_WhenAllNodesAreCollinear_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 1.0)
            .AddNode(2.0, 2.0)
            .Build("Collinear");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void GetMatrix_WhenTwoNodesCoincide_Throws()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .Build("Coincident nodes");

        // Act
        var act = () => _logic.GetMatrix(element);

        // Assert
        Assert.Throws<FeaCommonException>(() => act());
    }

    // ---------------------------------------------------------------------
    // Numerical edge cases
    // ---------------------------------------------------------------------

    [Test]
    public void GetMatrix_WhenThermalConductivityIsZero_ReturnsZeroMatrix()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 0.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Zero conductivity");

        // Act
        var matrix = _logic.GetMatrix(element);
        var nodes = element.Nodes.ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix.ValidateMatrixIsSymmetric(), Is.True);
            Assert.That(matrix.ValidateMatrixHasZeroRowSums(), Is.True);
        });

        foreach (var ni in nodes)
            foreach (var nj in nodes)
                Assert.That(matrix[ni, nj], Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void GetMatrix_WhenThicknessIsZero_ReturnsZeroMatrix()
    {
        // Arrange
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 0.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Zero thickness");

        // Act
        var matrix = _logic.GetMatrix(element);
        var nodes = element.Nodes.ToArray();

        // Assert
        foreach (var ni in nodes)
            foreach (var nj in nodes)
                Assert.That(matrix[ni, nj], Is.EqualTo(0.0).Within(Tolerance));
    }

    [Test]
    public void GetMatrix_WithVerySmallTriangle_IsFiniteAndSymmetric()
    {
        // Arrange
        const double h = 1e-6;
        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(h, 0.0)
            .AddNode(0.0, h)
            .Build("Tiny triangle");

        // Act
        var matrix = _logic.GetMatrix(element);
        var nodes = element.Nodes.ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix.ValidateMatrixIsSymmetric(), Is.True);
            Assert.That(matrix.ValidateMatrixHasZeroRowSums(), Is.True);
        });

        foreach (var ni in nodes)
            foreach (var nj in nodes)
                Assert.That(double.IsFinite(matrix[ni, nj]), Is.True);
    }

    // ---------------------------------------------------------------------
    // Geometry invariance
    // ---------------------------------------------------------------------

    [Test]
    public void GetMatrix_ForRightIsoscelesTriangle_MatchesAnalytical()
    {
        // Arrange
        double[,] expectedValues =
        {
            {  1.0, -0.5, -0.5 },
            { -0.5,  0.5,  0.0 },
            { -0.5,  0.0,  0.5 }
        };

        var element = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(0.0, 1.0)
            .Build("Right isosceles");

        // Act
        var matrix = _logic.GetMatrix(element);

        // Assert
        AssertMatrixMatchesExpected(element, matrix, expectedValues);
    }

    [Test]
    public void GetMatrix_IsInvariantToNodePermutation()
    {
        // Arrange
        var nodesBase = new (double x, double y)[]
        {
            (0.0, 0.0), (2.0, 0.0), (0.5, 1.5)
        };

        // Act
        var m0 = GetMatrixFor(nodesBase[0], nodesBase[1], nodesBase[2]);
        var m1 = GetMatrixFor(nodesBase[1], nodesBase[2], nodesBase[0]);
        var m2 = GetMatrixFor(nodesBase[2], nodesBase[0], nodesBase[1]);

        // Assert
        Assert.That(GetSortedEigenValues(m0), Is.EqualTo(GetSortedEigenValues(m1)).Within(1e-9));
        Assert.That(GetSortedEigenValues(m0), Is.EqualTo(GetSortedEigenValues(m2)).Within(1e-9));

        double[] GetSortedEigenValues(double[,] m)
        {
            var trace = m[0, 0] + m[1, 1] + m[2, 2];
            var minorSum =
                m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0] +
                m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1] +
                m[0, 0] * m[2, 2] - m[0, 2] * m[2, 0];
            var det =
                m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) -
                m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) +
                m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);

            return [trace, minorSum, det];
        }

        double[,] GetMatrixFor(params (double x, double y)[] pts)
        {
            var b = new FiniteElementBuilder()
                .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
                {
                    Thickness = 1.0,
                    ThermalConductivity = 1.0
                })
                .SetNodesType(ElementNodeType.Type2D);

            foreach (var (x, y) in pts)
                b.AddNode(x, y);

            var el = b.Build("Permutation test");
            var mat = _logic.GetMatrix(el);
            var ns = el.Nodes.ToArray();

            var result = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = mat[ns[i], ns[j]];

            return result;
        }
    }

    [Test]
    public void GetMatrix_ForScaledTriangle_IsInvariantToUniformScaling()
    {
        // Arrange
        var element1 = BuildTriangle((0, 0), (1, 0), (0, 1));
        var element2 = BuildTriangle((0, 0), (2, 0), (0, 2));

        // Act
        var m1 = GetMatrixArray(element1);
        var m2 = GetMatrixArray(element2);

        // Assert
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Assert.That(m2[i, j], Is.EqualTo(m1[i, j]).Within(1e-9),
                    $"Mismatch at [{i},{j}] under uniform scaling.");
    }

    // ---------------------------------------------------------------------
    // Statefulness / reusability
    // ---------------------------------------------------------------------

    [Test]
    public void GetMatrix_CalledTwiceOnSameLogicInstance_ProducesIndependentMatrices()
    {
        // Arrange
        var element1 = BuildTriangle((0, 0), (1, 0), (0, 1));
        var element2 = BuildTriangle((0, 0), (2, 0), (0, 2));
        var element3 = BuildTriangle((0, 0), (3, 0), (0, 3));

        // Act
        var m1 = _logic.GetMatrix(element1);
        _ = _logic.GetMatrix(element2);
        _ = _logic.GetMatrix(element3);

        // Assert
        var n1 = element1.Nodes.ToArray();

        Assert.That(m1[n1[0], n1[1]], Is.EqualTo(-0.5).Within(1e-9),
            "Previously returned matrix must not be mutated by subsequent calls.");
    }


    // ---------- Helpers ----------

    private static void AssertMatrixIsValid(IStiffnessMatrix matrix)
    {
        Assert.Multiple(() =>
        {
            Assert.That(matrix.ValidateMatrixIsSymmetric(), Is.True,
                "Stiffness matrix must be symmetric.");
            Assert.That(matrix.ValidateMatrixHasZeroRowSums(), Is.True,
                "Stiffness matrix rows must sum to zero.");
        });
    }

    private static IFiniteElement BuildTriangle(
        (double x, double y) a, (double x, double y) b, (double x, double y) c)
    {
        // Arrange & Act (factory helper — no assertion)
        return new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                Thickness = 1.0,
                ThermalConductivity = 1.0
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(a.x, a.y)
            .AddNode(b.x, b.y)
            .AddNode(c.x, c.y)
            .Build("Test triangle");
    }

    private static double[,] GetMatrixArray(IFiniteElement element)
    {
        var logic = new StiffnessMatrixTriangle2DTemperatureLogic();
        var matrix = logic.GetMatrix(element);
        var nodes = element.Nodes.ToArray();
        var result = new double[3, 3];
        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            result[i, j] = matrix[nodes[i], nodes[j]];
        return result;
    }

    private static void AssertMatrixMatchesExpected(
        IFiniteElement element,
        IStiffnessMatrix matrix,
        double[,] expected)
    {
        var nodes = element.Nodes.ToArray();
        for (int i = 0; i < nodes.Length; i++)
        for (int j = 0; j < nodes.Length; j++)
            Assert.That(
                matrix[nodes[i], nodes[j]],
                Is.EqualTo(expected[i, j]).Within(Tolerance),
                $"Mismatch at row={i}, col={j}.");
    }
}