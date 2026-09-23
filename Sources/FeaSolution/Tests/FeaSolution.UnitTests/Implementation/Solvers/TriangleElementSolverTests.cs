using FeaSolution.Implementation.Solvers;

namespace FeaSolution.UnitTests.Implementation.Solvers;

[TestFixture]
public class TriangleElementSolverTests
{
    [Test]
    public void E1_MatchesReferenceMatrix()
    {
        var e1 = new TriangleElementSolver(0, 1, 2, 2, 2, 0, 1.0, 1.0);

        Assert.That(e1.Area, Is.EqualTo(2.0).Within(1e-12));

        double[,] expected =
        {
            {  0.500, -0.250, -0.250 },
            { -0.250,  0.625, -0.375 },
            { -0.250, -0.375,  0.625 }
        };

        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            Assert.That(e1.Ke[i, j],
                Is.EqualTo(expected[i, j]).Within(1e-12));
    }

    [Test]
    public void E2_MatchesReferenceMatrix()
    {
        var e1 = new TriangleElementSolver(2, 2, 4, 1, 2, 0, 1.0, 1.0);

        Assert.That(e1.Area, Is.EqualTo(2.0).Within(1e-12));

        double[,] expected =
        {
            {  0.625, -0.250, -0.375 },
            { -0.250,  0.500, -0.250 },
            { -0.375, -0.250,  0.625 }
        };

        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            Assert.That(e1.Ke[i, j],
                Is.EqualTo(expected[i, j]).Within(1e-12));
    }

    [Test]
    public void E1_SymmetryAndZeroRowSums()
    {
        var e1 = new TriangleElementSolver(0, 1, 2, 2, 2, 0, 1.0, 1.0);
        Assert.IsTrue(e1.ValidateIsSymmetric());
        Assert.IsTrue(e1.ValidateHasZeroRowSums());
    }
}