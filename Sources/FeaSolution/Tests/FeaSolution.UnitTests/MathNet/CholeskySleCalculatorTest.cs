using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.MathNet.SleCalculation;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

/// <summary>
/// Tests for <see cref="CholeskySleCalculator"/>.
///
/// Cholesky decomposition requires the matrix to be symmetric positive
/// definite (SPD). All functional tests use SPD matrices — tridiagonal,
/// diagonal, or identity. Singular or non-SPD cases are covered separately
/// and expect <see cref="FeaCommonException"/>.
/// </summary>
[TestFixture]
[TestOf(typeof(CholeskySleCalculator))]
public class CholeskySleCalculatorTest : SleCalculatorTestBase
{
    private CholeskySleCalculator _calculator = null!;

    [SetUp]
    public void SetUp()
    {
        _calculator = new CholeskySleCalculator();
    }

    // ===============================================================
    // 1. Reference case: T3 from the report (4×4, SPD after Dirichlet)
    // ===============================================================

    [Test]
    public void SolveSLE_WhenT3ReferenceCase_ReturnsExpectedTemperatures()
    {
        // Arrange
        // K_D after applying Dirichlet conditions (T1 = 20, T4 = 100):
        // [[1.000,  0.000,  0.000, 0.000],
        //  [0.000,  1.250, -0.750, 0.000],
        //  [0.000, -0.750,  1.250, 0.000],
        //  [0.000,  0.000,  0.000, 1.000]]
        // SPD: symmetric ✔, all leading principal minors > 0 ✔.
        var nodes = MakeNodes(4);
        var matrixK = MakeMatrixMock(4, nodes, [
            (0, 0,  1.000),
            (1, 1,  1.250),
            (1, 2, -0.750),
            (2, 2,  1.250),
            (3, 3,  1.000)
        ]);

        var vectorF = MakeVector(nodes, 20.0, 30.0, 30.0, 100.0);

        var expectedT = new[] { 20.0, 60.0, 60.0, 100.0 };

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4));

        Assert.Multiple(() =>
        {
            for (var i = 0; i < expectedT.Length; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Temperature mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 2. Result items reference the correct nodes and Freedom
    // ===============================================================

    [Test]
    public void SolveSLE_WhenCorrectInput_ResultItemsReferenceCorrectNodesAndFreedom()
    {
        // Arrange
        var nodes = MakeNodes(3);
        var matrixK = MakeMatrixMock(3, nodes, DiagonalMatrix(1.0, 1.0, 1.0));
        var vectorF = MakeVector(nodes, 10.0, 20.0, 30.0);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                Assert.That(result[i].Node, Is.SameAs(nodes[i]),
                    $"Node reference mismatch at index {i}");
                Assert.That(result[i].Freedom, Is.EqualTo(Freedom.Temperature),
                    $"Freedom mismatch at index {i}");
            }
        });
    }

    // ===============================================================
    // 3. Identity matrix → solution equals RHS
    // ===============================================================

    [Test]
    public void SolveSLE_WhenIdentityMatrix_ReturnsRightHandSide()
    {
        // Arrange
        const int size = 5;
        var nodes = MakeNodes(size);
        var matrixK = MakeMatrixMock(size, nodes, DiagonalMatrix(1, 1, 1, 1, 1));

        var expectedT = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };
        var vectorF = MakeVector(nodes, expectedT);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 4. Diagonal matrix → element-wise division
    // ===============================================================

    [Test]
    public void SolveSLE_WhenDiagonalMatrix_ReturnsElementwiseDivision()
    {
        // Arrange
        // K = diag(2, 4, 5), F = [6, 8, 10] → T = [3, 2, 2]
        var nodes = MakeNodes(3);
        var matrixK = MakeMatrixMock(3, nodes, DiagonalMatrix(2.0, 4.0, 5.0));
        var vectorF = MakeVector(nodes, 6.0, 8.0, 10.0);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Value, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(result[1].Value, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(result[2].Value, Is.EqualTo(2.0).Within(Tolerance));
        });
    }

    // ===============================================================
    // 5. SPD tridiagonal matrix with known solution
    // ===============================================================

    [Test]
    public void SolveSLE_WhenTridiagonalSpdMatrix_ReturnsKnownSolution()
    {
        // Arrange
        // Tridiagonal SPD: K[i,i] = 2, K[i,i+1] = -1.
        // Known solution T = [1, 2, ..., N], computed via F = K·T.
        const int size = 11;
        var nodes = MakeNodes(size);
        var values = MakeTridiagonalMatrixValues(size);
        var matrixK = MakeMatrixMock(size, nodes, values);

        var expectedT = Enumerable.Range(1, size).Select(i => (double)i).ToArray();
        var f = MultiplyMatrixVector(values, expectedT, size);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 6. Quadratic solution on tridiagonal SPD
    // ===============================================================

    [Test]
    public void SolveSLE_WhenTridiagonalSpdMatrix_ReturnsQuadraticSolution()
    {
        // Arrange
        const int size = 11;
        var nodes = MakeNodes(size);
        var values = MakeTridiagonalMatrixValues(size);
        var matrixK = MakeMatrixMock(size, nodes, values);

        // Desired T_i = i²
        var expectedT = Enumerable.Range(1, size)
            .Select(i => (double)(i * i))
            .ToArray();

        var f = MultiplyMatrixVector(values, expectedT, size);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 7. Dirichlet-pattern matrix (fixed nodes with identity rows/cols)
    // ===============================================================

    [Test]
    public void SolveSLE_WhenDirichletPatternMatrix_ReturnsKnownSolution()
    {
        // Arrange
        // Nodes 1 and N are fixed (K = 1, off-diagonal = 0);
        // nodes 2..N-1 are tridiagonal SPD.
        const int size = 11;
        var nodes = MakeNodes(size);

        var values = new List<(int, int, double)>
        {
            (0, 0, 1.0),
            (size - 1, size - 1, 1.0)
        };
        for (var i = 1; i <= size - 2; i++)
            values.Add((i, i, 2.0));
        for (var i = 1; i <= size - 3; i++)
            values.Add((i, i + 1, -1.0));

        var matrixK = MakeMatrixMock(size, nodes, values);

        // Desired solution: fixed ends at 20 and 100, smooth inside.
        var expectedT = new double[]
        {
            20.0, 32.5, 44.0, 54.5, 64.0, 72.5, 80.0, 86.5, 92.0, 96.5, 100.0
        };

        var f = MultiplyMatrixVector(values, expectedT, size);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 8. Zero RHS → zero solution
    // ===============================================================

    [Test]
    public void SolveSLE_WhenZeroRhs_ReturnsZeroVector()
    {
        // Arrange
        const int size = 11;
        var nodes = MakeNodes(size);
        var matrixK = MakeMatrixMock(size, nodes, MakeTridiagonalMatrixValues(size));
        var vectorF = MakeVector(nodes, new double[size]);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result.All(r => r.Value == 0.0), Is.True,
            "All solution values must be zero when RHS is zero");
    }

    // ===============================================================
    // 9. 1×1 matrix — boundary case
    // ===============================================================

    [Test]
    public void SolveSLE_When1x1Matrix_ReturnsScalarDivision()
    {
        // Arrange
        var nodes = MakeNodes(1);
        var matrixK = MakeMatrixMock(1, nodes, DiagonalMatrix(2.0));
        var vectorF = MakeVector(nodes, 6.0);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].Value, Is.EqualTo(3.0).Within(Tolerance));
    }

    // ===============================================================
    // 10. 2×2 SPD matrix — simple non-trivial case
    // ===============================================================

    [Test]
    public void SolveSLE_When2x2SpdMatrix_ReturnsExpectedSolution()
    {
        // Arrange
        // K = [[4, 1], [1, 3]], F = [1, 2]
        // Solution: 4x + y = 1, x + 3y = 2
        //   y = 1 - 4x
        //   x + 3(1 - 4x) = 2 → -11x = -1 → x = 1/11
        //   y = 1 - 4/11 = 7/11
        var nodes = MakeNodes(2);
        var matrixK = MakeMatrixMock(2, nodes, [
            (0, 0, 4.0),
            (0, 1, 1.0),
            (1, 1, 3.0)
        ]);

        var vectorF = MakeVector(nodes, 1.0, 2.0);

        var expectedT = new[] { 1.0 / 11.0, 7.0 / 11.0 };

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Value, Is.EqualTo(expectedT[0]).Within(Tolerance));
            Assert.That(result[1].Value, Is.EqualTo(expectedT[1]).Within(Tolerance));
        });
    }

    // ===============================================================
    // 11. Result order matches matrix.Nodes order
    // ===============================================================

    [Test]
    public void SolveSLE_WhenCorrectInput_ResultOrderMatchesNodesOrder()
    {
        // Arrange
        var nodes = MakeNodes(5);
        var matrixK = MakeMatrixMock(5, nodes, DiagonalMatrix(1, 1, 1, 1, 1));
        var vectorF = MakeVector(nodes, 1.0, 2.0, 3.0, 4.0, 5.0);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < nodes.Length; i++)
                Assert.That(result[i].Node, Is.SameAs(nodes[i]),
                    $"Node order mismatch at index {i}");
        });
    }

    // ===============================================================
    // 12. Non-SPD matrix → FeaCommonException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenMatrixIsNotSpd_ThrowsFeaCommonException()
    {
        // Arrange
        // Symmetric but not positive definite:
        // K = [[1, 2], [2, 1]] — eigenvalues 3 and -1.
        var nodes = MakeNodes(2);
        var matrixK = MakeMatrixMock(2, nodes, [
            (0, 0, 1.0),
            (0, 1, 2.0),
            (1, 1, 1.0)
        ]);

        var vectorF = MakeVector(nodes, 1.0, 1.0);

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixK, vectorF));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("positive definite").IgnoreCase);
    }

    // ===============================================================
    // 13. Singular (zero diagonal) matrix → FeaCommonException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenMatrixIsSingular_ThrowsFeaCommonException()
    {
        // Arrange
        // Diagonal matrix with a zero on the diagonal — not SPD.
        var nodes = MakeNodes(3);
        var matrixK = MakeMatrixMock(3, nodes, DiagonalMatrix(1.0, 0.0, 1.0));
        var vectorF = MakeVector(nodes, 1.0, 1.0, 1.0);

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Does.Contain("positive definite").IgnoreCase);
    }

    // ===============================================================
    // 14. Zero-size matrix → FeaCommonException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenMatrixIsEmpty_ThrowsFeaCommonException()
    {
        // Arrange
        var matrixK = MakeMatrixMock(0, [], []);
        var vectorF = Array.Empty<SolutionItem>();

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The size of source matrix is zero."));
    }

    // ===============================================================
    // 15. Null arguments → ArgumentNullException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenMatrixIsNull_ThrowsArgumentNullException()
    {
        var vectorF = Array.Empty<SolutionItem>();

        Assert.Throws<ArgumentNullException>(
            () => _calculator.SolveSLE(null!, vectorF));
    }

    [Test]
    public void SolveSLE_WhenVectorIsNull_ThrowsArgumentNullException()
    {
        var nodes = MakeNodes(1);
        var matrixK = MakeMatrixMock(1, nodes, DiagonalMatrix(1.0));

        Assert.Throws<ArgumentNullException>(
            () => _calculator.SolveSLE(matrixK, null!));
    }

    // ===============================================================
    // 16. Dimension mismatch → FeaCommonException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenVectorLengthMismatch_ThrowsFeaCommonException()
    {
        // Arrange
        var nodes = MakeNodes(3);
        var matrixK = MakeMatrixMock(3, nodes, DiagonalMatrix(1, 1, 1));

        // Vector shorter than matrix size
        var vectorF = MakeVector(nodes.Take(2).ToArray(), 1.0, 1.0);

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Does.Contain("Dimension mismatch"));
    }

    // ===============================================================
    // 17. Node order mismatch → FeaCommonException
    // ===============================================================

    [Test]
    public void SolveSLE_WhenNodeOrderMismatch_ThrowsFeaCommonException()
    {
        // Arrange
        var n1 = MakeNodeMock("node1");
        var n2 = MakeNodeMock("node2");
        var nodes = new[] { n1, n2 };

        var matrixK = MakeMatrixMock(2, nodes, DiagonalMatrix(1.0, 1.0));

        // Vector F is in reverse order
        var vectorF = new[]
        {
            MakeItem(n2, 1.0),
            MakeItem(n1, 1.0)
        };

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _calculator.SolveSLE(matrixK, vectorF));

        // Assert
        Assert.That(ex!.Message, Does.Contain("Node order mismatch"));
    }

    // ===============================================================
    // 18. Reads NonZeroMatrixValuesWithIndexes exactly once
    // ===============================================================

    [Test]
    public void SolveSLE_WhenCorrectInput_ReadsNonZeroValuesFromMatrixOnce()
    {
        // Arrange
        var nodes = MakeNodes(3);
        var matrixK = MakeMatrixMock(3, nodes, DiagonalMatrix(1, 1, 1));
        var vectorF = MakeVector(nodes, 1.0, 2.0, 3.0);

        // Act
        _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        matrixK.Received(1).NonZeroMatrixValuesWithIndexes();
        _ = matrixK.Received(2).Nodes;
    }

    // ===============================================================
    // 20. Large SPD matrix — sanity check
    // ===============================================================

    [Test]
    public void SolveSLE_WhenLargeTridiagonalSpdMatrix_ReturnsExpectedSolution()
    {
        // Arrange
        const int size = 200;
        var nodes = MakeNodes(size);
        var values = MakeTridiagonalMatrixValues(size);
        var matrixK = MakeMatrixMock(size, nodes, values);

        var expectedT = Enumerable.Range(1, size)
            .Select(i => (double)i)
            .ToArray();

        var f = MultiplyMatrixVector(values, expectedT, size);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.That(result.Length, Is.EqualTo(size));

        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(1e-6),
                    $"Mismatch at node {i + 1}");
        });
    }

    // ===============================================================
    // 21. Solution satisfies K·T = F (indirect correctness check)
    // ===============================================================

    [Test]
    public void SolveSLE_WhenCorrectInput_ResultSatisfiesOriginalEquation()
    {
        // Arrange
        const int size = 11;
        var nodes = MakeNodes(size);
        var values = MakeTridiagonalMatrixValues(size);
        var matrixK = MakeMatrixMock(size, nodes, values);

        var vectorF = MakeVector(nodes, Enumerable.Repeat(1.0, size).ToArray());

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert: compute K·T and compare with F
        var computedF = MultiplyMatrixVector(values,
            result.Select(r => r.Value).ToArray(), size);

        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(computedF[i], Is.EqualTo(vectorF[i].Value).Within(Tolerance),
                    $"K·T mismatch at position {i + 1}: " +
                    $"got {computedF[i]}, expected {vectorF[i].Value}");
        });
    }

    // ===============================================================
    // 22. Implements ISleCalculator
    // ===============================================================

    [Test]
    public void CholeskySleCalculator_ImplementsISleCalculator()
    {
        Assert.That(_calculator, Is.InstanceOf<ISleCalculator>());
    }

    // ===============================================================
    // 23. Symmetric input without explicit lower triangle
    // ===============================================================

    [Test]
    public void SolveSLE_WhenOnlyUpperTriangleProvided_SymmetrizesCorrectly()
    {
        // Arrange
        // Only upper triangle is given (as StiffnessMatrix does).
        // The base class BuildMathNetDenseMatrix must symmetrize it.
        const int size = 5;
        var nodes = MakeNodes(size);
        var values = MakeTridiagonalMatrixValues(size);   // upper triangle only
        var matrixK = MakeMatrixMock(size, nodes, values);

        var expectedT = new double[] { 1, 2, 3, 4, 5 };
        var f = MultiplyMatrixVector(values, expectedT, size);
        var vectorF = MakeVector(nodes, f);

        // Act
        var result = _calculator.SolveSLE(matrixK, vectorF);

        // Assert
        Assert.Multiple(() =>
        {
            for (var i = 0; i < size; i++)
                Assert.That(result[i].Value, Is.EqualTo(expectedT[i]).Within(Tolerance),
                    $"Mismatch at node {i + 1}");
        });
    }
}