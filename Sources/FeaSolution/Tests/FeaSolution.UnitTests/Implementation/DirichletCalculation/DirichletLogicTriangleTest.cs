using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.DirichletCalculation;
using FeaSolution.Implementation.StiffnessMatrixs;
using NSubstitute;

namespace FeaSolution.UnitTests.Implementation.DirichletCalculation;

/// <summary>
/// Тесты с матрицей 3×3.
/// </summary>
[TestFixture]
[TestOf(typeof(DirichletLogic))]
public class DirichletLogicTriangleTest
{
    private DirichletLogic _dirichletLogic = null!;

    private IElementNode _a = null!;
    private IElementNode _b = null!;
    private IElementNode _c = null!;

    [SetUp]
    public void SetUp()
    {
        _dirichletLogic = new DirichletLogic();

        _a = CreateNodeMock("a");
        _b = CreateNodeMock("b");
        _c = CreateNodeMock("c");
    }

    [Test]
    public void ApplyDirichlet_Triangle_ConstraintOnA_ProducesExpectedMatrix()
    {
        // Arrange
        var k = CreateTriangleMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_a, 10.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — матрица K'
        Assert.Multiple(() =>
        {
            Assert.That(k[_a, _a], Is.EqualTo(1.0));
            Assert.That(k[_a, _b], Is.EqualTo(0.0));
            Assert.That(k[_a, _c], Is.EqualTo(0.0));

            Assert.That(k[_b, _a], Is.EqualTo(0.0));
            Assert.That(k[_c, _a], Is.EqualTo(0.0));

            Assert.That(k[_b, _b], Is.EqualTo(1.0));
            Assert.That(k[_c, _c], Is.EqualTo(1.0));
            Assert.That(k[_b, _c], Is.EqualTo(-0.5));
            Assert.That(k[_c, _b], Is.EqualTo(-0.5));
        });
    }

    [Test]
    public void ApplyDirichlet_Triangle_ConstraintOnA_ProducesExpectedRhs()
    {
        // Arrange
        var k = CreateTriangleMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_a, 10.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — F' = [10, 5, 5]
        // Fb = 0 - (-0.5)*10 = 5
        // Fc = 0 - (-0.5)*10 = 5
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(10.0));
            Assert.That(f[1].Value, Is.EqualTo(5.0));
            Assert.That(f[2].Value, Is.EqualTo(5.0));
        });
    }

    [Test]
    public void ApplyDirichlet_Triangle_TwoConstraints_ProducesExpectedRhs()
    {
        // Arrange — Ta = 10, Tc = 40
        var k = CreateTriangleMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_a, 10.0),
            MakeCondition(_c, 40.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert
        // После Ta=10: F = [10, 5, 5]
        // После Tc=40: Fb = 5 - (-0.5)*40 = 25
        Assert.Multiple(() =>
        {
            Assert.That(k[_a, _a], Is.EqualTo(1.0));
            Assert.That(k[_c, _c], Is.EqualTo(1.0));
            Assert.That(k[_a, _c], Is.EqualTo(0.0));
            Assert.That(k[_b, _c], Is.EqualTo(0.0));

            Assert.That(f[0].Value, Is.EqualTo(10.0));
            Assert.That(f[1].Value, Is.EqualTo(25.0));
            Assert.That(f[2].Value, Is.EqualTo(40.0));
        });
    }

    private static DirichleCondition MakeCondition(IElementNode node, double value)
        => new()
        {
            Node = node,
            Freedom = Freedom.Temperature,
            Value = value
        };

    private static IElementNode CreateNodeMock(string userId)
    {
        var node = Substitute.For<IElementNode>();
        node.UserId.Returns(userId);
        return node;
    }

    /// <summary>
    /// Матрица 3×3:
    /// [[ 1.0, -0.5, -0.5],
    ///  [-0.5,  1.0, -0.5],
    ///  [-0.5, -0.5,  1.0]]
    /// </summary>
    private StiffnessMatrix CreateTriangleMatrix()
    {
        var k = new StiffnessMatrix();

        k.AddValue(_a, _a, 1.0);
        k.AddValue(_b, _b, 1.0);
        k.AddValue(_c, _c, 1.0);

        k.AddValue(_a, _b, -0.5);
        k.AddValue(_a, _c, -0.5);
        k.AddValue(_b, _c, -0.5);

        return k;
    }

    private SolutionItem[] CreateZeroVectorF()
        =>
        [
            new() { Node = _a, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _b, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _c, Freedom = Freedom.Temperature, Value = 0.0 }
        ];
}