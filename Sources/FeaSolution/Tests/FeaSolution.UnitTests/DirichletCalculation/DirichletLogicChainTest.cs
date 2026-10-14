using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.DirichletCalculation;
using FeaSolution.Implementation.StiffnessMatrixs;
using NSubstitute;

namespace FeaSolution.UnitTests.DirichletCalculation;

/// <summary>
/// Тесты с трёхдиагональной матрицей 5×5 (1D-цепочка).
/// </summary>
[TestFixture]
[TestOf(typeof(DirichletLogic))]
public class DirichletLogicChainTest
{
    private DirichletLogic _dirichletLogic = null!;

    private IElementNode _n1 = null!;
    private IElementNode _n2 = null!;
    private IElementNode _n3 = null!;
    private IElementNode _n4 = null!;
    private IElementNode _n5 = null!;

    private IElementNode[] _nodes = null!;

    [SetUp]
    public void SetUp()
    {
        _dirichletLogic = new DirichletLogic();

        _n1 = CreateNodeMock("n1");
        _n2 = CreateNodeMock("n2");
        _n3 = CreateNodeMock("n3");
        _n4 = CreateNodeMock("n4");
        _n5 = CreateNodeMock("n5");

        _nodes = [_n1, _n2, _n3, _n4, _n5];
    }

    [Test]
    public void ApplyDirichlet_Chain_TwoBoundaryConditions_ProducesExpectedMatrix()
    {
        // Arrange
        var k = CreateChainMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_n1, 20.0),
            MakeCondition(_n5, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — матрица K_D
        Assert.Multiple(() =>
        {
            // Строка/столбец 1 — занулены, диагональ = 1
            Assert.That(k[_n1, _n1], Is.EqualTo(1.0));
            Assert.That(k[_n1, _n2], Is.EqualTo(0.0));
            Assert.That(k[_n2, _n1], Is.EqualTo(0.0));

            // Строка/столбец 5 — занулены, диагональ = 1
            Assert.That(k[_n5, _n5], Is.EqualTo(1.0));
            Assert.That(k[_n5, _n4], Is.EqualTo(0.0));
            Assert.That(k[_n4, _n5], Is.EqualTo(0.0));

            // Центральный блок 3×3 не тронут
            Assert.That(k[_n2, _n2], Is.EqualTo(2.0));
            Assert.That(k[_n3, _n3], Is.EqualTo(2.0));
            Assert.That(k[_n4, _n4], Is.EqualTo(2.0));
            Assert.That(k[_n2, _n3], Is.EqualTo(-1.0));
            Assert.That(k[_n3, _n4], Is.EqualTo(-1.0));
            Assert.That(k[_n3, _n2], Is.EqualTo(-1.0));
            Assert.That(k[_n4, _n3], Is.EqualTo(-1.0));
        });
    }

    [Test]
    public void ApplyDirichlet_Chain_TwoBoundaryConditions_ProducesExpectedRhs()
    {
        // Arrange
        var k = CreateChainMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_n1, 20.0),
            MakeCondition(_n5, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — F_D = [20, 20, 0, 100, 100]
        // После T1=20: F = [20, 20, 0, 0, 0]
        // После T5=100: F4 = 0 - (-1)*100 = 100, F = [20, 20, 0, 100, 100]
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0));
            Assert.That(f[1].Value, Is.EqualTo(20.0));
            Assert.That(f[2].Value, Is.EqualTo(0.0));
            Assert.That(f[3].Value, Is.EqualTo(100.0));
            Assert.That(f[4].Value, Is.EqualTo(100.0));
        });
    }

    [Test]
    public void ApplyDirichlet_Chain_SingleConstraintOnN1_ProducesExpectedRhs()
    {
        // Arrange
        var k = CreateChainMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_n1, 20.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — только F2 получает вклад: 0 - (-1)*20 = 20
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0));
            Assert.That(f[1].Value, Is.EqualTo(20.0));
            Assert.That(f[2].Value, Is.EqualTo(0.0));
            Assert.That(f[3].Value, Is.EqualTo(0.0));
            Assert.That(f[4].Value, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void ApplyDirichlet_Chain_AllNodesConstrained_ProducesIdentityMatrix()
    {
        // Arrange — все пять узлов заданы
        var k = CreateChainMatrix();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichleCondition>
        {
            MakeCondition(_n1, 20.0),
            MakeCondition(_n2, 40.0),
            MakeCondition(_n3, 60.0),
            MakeCondition(_n4, 80.0),
            MakeCondition(_n5, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — матрица стала единичной, вектор содержит заданные значения
        Assert.Multiple(() =>
        {
            for (var i = 0; i < _nodes.Length; i++)
            {
                for (var j = 0; j < _nodes.Length; j++)
                {
                    var expected = i == j ? 1.0 : 0.0;
                    Assert.That(k[_nodes[i], _nodes[j]], Is.EqualTo(expected),
                        $"K[{_nodes[i].UserId}, {_nodes[j].UserId}] must be {(i == j ? "1" : "0")}.");
                }
            }

            Assert.That(f[0].Value, Is.EqualTo(20.0));
            Assert.That(f[1].Value, Is.EqualTo(40.0));
            Assert.That(f[2].Value, Is.EqualTo(60.0));
            Assert.That(f[3].Value, Is.EqualTo(80.0));
            Assert.That(f[4].Value, Is.EqualTo(100.0));
        });
    }

    [Test]
    public void ApplyDirichlet_Chain_ConditionOrderDoesNotMatter()
    {
        // Arrange
        var kFirst = CreateChainMatrix();
        var fFirst = CreateZeroVectorF();
        var conditionsFirst = new List<DirichleCondition>
        {
            MakeCondition(_n1, 20.0),
            MakeCondition(_n5, 100.0)
        };

        var kSecond = CreateChainMatrix();
        var fSecond = CreateZeroVectorF();
        var conditionsSecond = new List<DirichleCondition>
        {
            MakeCondition(_n5, 100.0),
            MakeCondition(_n1, 20.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(kFirst, fFirst, conditionsFirst);
        _dirichletLogic.ApplyDirichlet(kSecond, fSecond, conditionsSecond);

        // Assert
        Assert.Multiple(() =>
        {
            foreach (var i in _nodes)
            {
                foreach (var j in _nodes)
                {
                    Assert.That(kSecond[i, j], Is.EqualTo(kFirst[i, j]),
                        $"K[{i.UserId}, {j.UserId}] must be equal regardless of condition order.");
                }
            }

            for (var i = 0; i < fFirst.Length; i++)
            {
                Assert.That(fSecond[i].Value, Is.EqualTo(fFirst[i].Value),
                    $"F[{i}].Value must be equal regardless of condition order.");
            }
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
    /// Трёхдиагональная матрица 5×5 (1D-цепочка):
    /// [[ 2, -1,  0,  0,  0],
    ///  [-1,  2, -1,  0,  0],
    ///  [ 0, -1,  2, -1,  0],
    ///  [ 0,  0, -1,  2, -1],
    ///  [ 0,  0,  0, -1,  2]]
    /// </summary>
    private StiffnessMatrix CreateChainMatrix()
    {
        var k = new StiffnessMatrix();

        // Диагональ
        for (var i = 0; i < _nodes.Length; i++)
            k.AddValue(_nodes[i], _nodes[i], 2.0);

        // Внедиагональные — соседние
        for (var i = 0; i < _nodes.Length - 1; i++)
            k.AddValue(_nodes[i], _nodes[i + 1], -1.0);

        return k;
    }

    private SolutionItem[] CreateZeroVectorF()
        =>
        [
            new() { Node = _n1, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _n2, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _n3, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _n4, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _n5, Freedom = Freedom.Temperature, Value = 0.0 }
        ];
}