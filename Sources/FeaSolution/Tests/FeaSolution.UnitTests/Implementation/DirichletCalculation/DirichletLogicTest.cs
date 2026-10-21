using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.DirichletCalculation;
using FeaSolution.Implementation.StiffnessMatrixs;
using NSubstitute;

namespace FeaSolution.UnitTests.Implementation.DirichletCalculation;

[TestFixture]
[TestOf(typeof(DirichletLogic))]
public class DirichletLogicTest
{
    private DirichletLogic _dirichletLogic = null!;

    private IElementNode _node1 = null!;
    private IElementNode _node2 = null!;
    private IElementNode _node3 = null!;
    private IElementNode _node4 = null!;

    private IElementNode[] _nodes = null!;

    [SetUp]
    public void SetUp()
    {
        _dirichletLogic = new DirichletLogic();

        _node1 = CreateNodeMock("node1");
        _node2 = CreateNodeMock("node2");
        _node3 = CreateNodeMock("node3");
        _node4 = CreateNodeMock("node4");

        _nodes = [_node1, _node2, _node3, _node4];
    }

    // =====================================================================
    // Сценарий одного условия Дирихле: T1 = 20 К
    // Матрица K':
    // [[1.000, 0.000, 0.000, 0.000],
    //  [0.000, 1.250, -0.750, -0.250],
    //  [0.000, -0.750, 1.250, -0.250],
    //  [0.000, -0.250, -0.250, 0.500]]
    // Вектор F' = [20, 5, 5, 0]
    // =====================================================================

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_SetsDiagonalToOne(string nodeKey)
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert
        Assert.That(k[node, node], Is.EqualTo(1.000).Within(1e-10));
    }

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_ZeroesRow(string nodeKey)
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — все внедиагональные элементы строки заданного узла равны нулю
        foreach (var other in _nodes)
        {
            if (ReferenceEquals(other, node)) continue;

            Assert.That(k[node, other], Is.EqualTo(0.0).Within(1e-10),
                $"K[{nodeKey}, {other.NodeId}] must be zero after Dirichlet.");
        }
    }

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_ZeroesColumn(string nodeKey)
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — все внедиагональные элементы столбца заданного узла равны нулю
        foreach (var other in _nodes)
        {
            if (ReferenceEquals(other, node)) continue;

            Assert.That(k[other, node], Is.EqualTo(0.0).Within(1e-10),
                $"K[{other.NodeId}, {nodeKey}] must be zero after Dirichlet.");
        }
    }

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_DoesNotTouchOtherRowsAndColumns(string nodeKey)
    {
        // Arrange
        var kActual = CreateMatrixK();
        var kExpected = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(kActual, f, condition);

        // Assert — все пары вне строки/столбца node совпадают с эталоном
        Assert.Multiple(() =>
        {
            foreach (var i in _nodes)
            {
                if (ReferenceEquals(i, node)) continue;

                foreach (var j in _nodes)
                {
                    if (ReferenceEquals(j, node)) continue;

                    Assert.That(kActual[i, j],
                        Is.EqualTo(kExpected[i, j]).Within(1e-10),
                        $"K[{i.NodeId}, {j.NodeId}] must not change after Dirichlet on {nodeKey}.");
                }
            }
        });
    }

    [Test]
    public void ApplyDirichletOnce_FirstCondition_ProducesExpectedMatrix()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node1, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — полная матрица K'
        Assert.Multiple(() =>
        {
            Assert.That(k[_node1, _node1], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node1, _node2], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node3], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node2, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node2, _node2], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node2, _node3], Is.EqualTo(-0.750).Within(1e-10));
            Assert.That(k[_node2, _node4], Is.EqualTo(-0.250).Within(1e-10));

            Assert.That(k[_node3, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node3, _node2], Is.EqualTo(-0.750).Within(1e-10));
            Assert.That(k[_node3, _node3], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node3, _node4], Is.EqualTo(-0.250).Within(1e-10));

            Assert.That(k[_node4, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node2], Is.EqualTo(-0.250).Within(1e-10));
            Assert.That(k[_node4, _node3], Is.EqualTo(-0.250).Within(1e-10));
            Assert.That(k[_node4, _node4], Is.EqualTo(0.500).Within(1e-10));
        });
    }

    // =====================================================================
    // Проверка вектора F' = [20, 5, 5, 0]
    // =====================================================================

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_SetsRhsAtConstrainedNode(string nodeKey)
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert 
        var constrainedItem = f.First(item => item.Node.NodeId == nodeKey);
        Assert.That(constrainedItem.Value, Is.EqualTo(20.0).Within(1e-10));
    }

    [Test]
    public void ApplyDirichletOnce_FirstCondition_UpdatesRhsWithKnownTemperatureContribution()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node1, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — F' = [20, 5, 5, 0]
        // F2 = 0 - (-0.250)*20 = 5
        // F3 = 0 - (-0.250)*20 = 5
        // F4 = 0 - ( 0.000)*20 = 0
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(5.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(5.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(0.0).Within(1e-10));
        });
    }

    [TestCase("node1")]
    [TestCase("node2")]
    [TestCase("node3")]
    [TestCase("node4")]
    public void ApplyDirichletOnce_AnyCondition_PreservesNodeAndFreedomInVector(string nodeKey)
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var node = ResolveNode(nodeKey);
        var condition = MakeCondition(node, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — Node и Freedom у всех элементов не изменились
        Assert.Multiple(() =>
        {
            for (var i = 0; i < f.Length; i++)
            {
                Assert.That(f[i].Node, Is.SameAs(_nodes[i]),
                    $"F[{i}].Node must stay {_nodes[i].NodeId}.");
                Assert.That(f[i].Freedom, Is.EqualTo(Freedom.Temperature),
                    $"F[{i}].Freedom must stay Temperature.");
            }
        });
    }

    // =====================================================================
    // Симметричный сценарий: T4 = 100 К
    // =====================================================================

    [Test]
    public void ApplyDirichletOnce_FourthCondition_ProducesExpectedMatrix()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node4, 100.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(k[_node4, _node4], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node4, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node2], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node3], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node1, _node4], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node2, _node4], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node3, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node1, _node1], Is.EqualTo(0.500).Within(1e-10));
            Assert.That(k[_node2, _node2], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node3, _node3], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node2, _node3], Is.EqualTo(-0.750).Within(1e-10));
        });
    }

    [Test]
    public void ApplyDirichletOnce_FourthCondition_UpdatesRhsWithKnownTemperatureContribution()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node4, 100.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — F' = [0, 25, 25, 100]
        // F1 = 0 - K[1,4]*100 = 0 - 0 = 0
        // F2 = 0 - K[2,4]*100 = 0 - (-0.250)*100 = 25
        // F3 = 0 - K[3,4]*100 = 0 - (-0.250)*100 = 25
        // F4 = 100
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(0.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(25.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(25.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(100.0).Within(1e-10));
        });
    }

    // =====================================================================
    // Проверка, что существующий вклад F сохраняется
    // =====================================================================

    [Test]
    public void ApplyDirichletOnce_FirstCondition_AddsToExistingRhs()
    {
        // Arrange — F уже содержит ненулевые значения
        var k = CreateMatrixK();
        var f = new[]
        {
            new SolutionItem { Node = _node1, Freedom = Freedom.Temperature, Value = 1.0 },
            new SolutionItem { Node = _node2, Freedom = Freedom.Temperature, Value = 2.0 },
            new SolutionItem { Node = _node3, Freedom = Freedom.Temperature, Value = 3.0 },
            new SolutionItem { Node = _node4, Freedom = Freedom.Temperature, Value = 4.0 }
        };
        var condition = MakeCondition(_node1, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — F' = [20, 2+5, 3+5, 4+0] = [20, 7, 8, 4]
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(7.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(8.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(4.0).Within(1e-10));
        });
    }

    // =====================================================================
    // Граничный случай: узел без связи (K[1,4] = 0)
    // =====================================================================

    [Test]
    public void ApplyDirichletOnce_ConditionOnIsolatedNode_LeavesMatrixStructureConsistent()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node1, 20.0);

        // Act
        _dirichletLogic.ApplyDirichletOnce(k, f, condition);

        // Assert — узел 4 не должен измениться, так как K[1,4] = 0
        Assert.Multiple(() =>
        {
            Assert.That(k[_node4, _node4], Is.EqualTo(0.500).Within(1e-10));
            Assert.That(k[_node4, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node4], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(0.0).Within(1e-10));
        });
    }

    // =====================================================================
    // Защита от null
    // =====================================================================

    [Test]
    public void ApplyDirichletOnce_ThrowsOnNullMatrix()
    {
        // Arrange
        var f = CreateZeroVectorF();
        var condition = MakeCondition(_node1, 20.0);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichletOnce(null!, f, condition));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'matrixK')"));
    }

    [Test]
    public void ApplyDirichletOnce_ThrowsOnNullVectorF()
    {
        // Arrange
        var k = CreateMatrixK();
        var condition = MakeCondition(_node1, 20.0);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichletOnce(k, null!, condition));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'vectorF')"));
    }

    [Test]
    public void ApplyDirichletOnce_ThrowsOnNullCondition()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichletOnce(k, f, null!));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'condition')"));
    }

    [Test]
    public void ApplyDirichlet_SingleCondition_ModifiesMatrixAndVector()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert 
        Assert.Multiple(() =>
        {
            Assert.That(k[_node1, _node1], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node1, _node2], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node3], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node2, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node3, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node1], Is.EqualTo(0.000).Within(1e-10));
        });

        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(5.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(5.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(0.0).Within(1e-10));
        });
    }
    
    [Test]
    public void ApplyDirichlet_TwoConditions_ProducesExpectedMatrix()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0),
            MakeCondition(_node4, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert 
        Assert.Multiple(() =>
        {
            Assert.That(k[_node1, _node1], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node1, _node2], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node3], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node1, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node2, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node2, _node2], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node2, _node3], Is.EqualTo(-0.750).Within(1e-10));
            Assert.That(k[_node2, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node3, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node3, _node2], Is.EqualTo(-0.750).Within(1e-10));
            Assert.That(k[_node3, _node3], Is.EqualTo(1.250).Within(1e-10));
            Assert.That(k[_node3, _node4], Is.EqualTo(0.000).Within(1e-10));

            Assert.That(k[_node4, _node1], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node2], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node3], Is.EqualTo(0.000).Within(1e-10));
            Assert.That(k[_node4, _node4], Is.EqualTo(1.000).Within(1e-10));
        });
    }

    [Test]
    public void ApplyDirichlet_TwoConditions_ProducesExpectedRhs()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0),
            MakeCondition(_node4, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert — F_D = [20, 30, 30, 100]
        // После T1=20: F = [20, 5, 5, 0]
        // После T4=100: F2 = 5 - (-0.250)*100 = 30, F3 = 5 - (-0.250)*100 = 30
        Assert.Multiple(() =>
        {
            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(30.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(30.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(100.0).Within(1e-10));
        });
    }

    [Test]
    public void ApplyDirichlet_TwoConditions_ConditionOrderDoesNotMatter()
    {
        // Arrange
        var kFirst = CreateMatrixK();
        var fFirst = CreateZeroVectorF();
        var conditionsFirst = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0),
            MakeCondition(_node4, 100.0)
        };

        var kSecond = CreateMatrixK();
        var fSecond = CreateZeroVectorF();
        var conditionsSecond = new List<DirichletCondition>
        {
            MakeCondition(_node4, 100.0),
            MakeCondition(_node1, 20.0)
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
                    Assert.That(kSecond[i, j], Is.EqualTo(kFirst[i, j]).Within(1e-10),
                        $"K[{i.NodeId}, {j.NodeId}] must be equal regardless of condition order.");
                }
            }

            for (var i = 0; i < fFirst.Length; i++)
            {
                Assert.That(fSecond[i].Value, Is.EqualTo(fFirst[i].Value).Within(1e-10),
                    $"F[{i}].Value must be equal regardless of condition order.");
            }
        });
    }

    [Test]
    public void ApplyDirichlet_ThreeConditions_AllConstrainedNodesAreSet()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0),
            MakeCondition(_node2, 40.0),
            MakeCondition(_node4, 100.0)
        };

        // Act
        _dirichletLogic.ApplyDirichlet(k, f, conditions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(k[_node1, _node1], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node2, _node2], Is.EqualTo(1.000).Within(1e-10));
            Assert.That(k[_node4, _node4], Is.EqualTo(1.000).Within(1e-10));

            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(40.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(100.0).Within(1e-10));
        });
    }

    [Test]
    public void ApplyDirichlet_AllFourConditions_FullyConstrainedSystem()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>
        {
            MakeCondition(_node1, 20.0),
            MakeCondition(_node2, 40.0),
            MakeCondition(_node3, 60.0),
            MakeCondition(_node4, 100.0)
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
                    Assert.That(k[_nodes[i], _nodes[j]], Is.EqualTo(expected).Within(1e-10),
                        $"K[{_nodes[i].NodeId}, {_nodes[j].NodeId}] must be {(i == j ? "1" : "0")}.");
                }
            }

            Assert.That(f[0].Value, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(f[1].Value, Is.EqualTo(40.0).Within(1e-10));
            Assert.That(f[2].Value, Is.EqualTo(60.0).Within(1e-10));
            Assert.That(f[3].Value, Is.EqualTo(100.0).Within(1e-10));
        });
    }

    [Test]
    public void ApplyDirichlet_EmptyConditions_ThrowsFeaCommonException()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition>();

        // Act
        var ex = Assert.Throws<FeaCommonException>(
            () => _dirichletLogic.ApplyDirichlet(k, f, conditions));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The collection of Dirichle condition is empty."));
    }

    [Test]
    public void ApplyDirichlet_ThrowsOnNullMatrix()
    {
        // Arrange
        var f = CreateZeroVectorF();
        var conditions = new List<DirichletCondition> { MakeCondition(_node1, 20.0) };

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichlet(null!, f, conditions));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'matrixK')"));
    }

    [Test]
    public void ApplyDirichlet_ThrowsOnNullVectorF()
    {
        // Arrange
        var k = CreateMatrixK();
        var conditions = new List<DirichletCondition> { MakeCondition(_node1, 20.0) };

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichlet(k, null!, conditions));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'vectorF')"));
    }

    [Test]
    public void ApplyDirichlet_ThrowsOnNullConditions()
    {
        // Arrange
        var k = CreateMatrixK();
        var f = CreateZeroVectorF();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => _dirichletLogic.ApplyDirichlet(k, f, null!));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Value cannot be null. (Parameter 'dirichleConditions')"));
    }
    
    /// <summary>
    /// Собирает глобальную матрицу K:
    /// [[ 0.500, -0.250, -0.250,  0.000],
    ///  [-0.250,  1.250, -0.750, -0.250],
    ///  [-0.250, -0.750,  1.250, -0.250],
    ///  [ 0.000, -0.250, -0.250,  0.500]]
    /// </summary>
    private StiffnessMatrix CreateMatrixK()
    {
        var k = new StiffnessMatrix();

        // Диагональ
        k.AddValue(_node1, _node1, 0.500);
        k.AddValue(_node2, _node2, 1.250);
        k.AddValue(_node3, _node3, 1.250);
        k.AddValue(_node4, _node4, 0.500);

        // Внедиагональные
        k.AddValue(_node1, _node2, -0.250);
        k.AddValue(_node1, _node3, -0.250);

        k.AddValue(_node2, _node3, -0.750);
        k.AddValue(_node2, _node4, -0.250);

        k.AddValue(_node3, _node4, -0.250);

        return k;
    }

    /// <summary>
    /// Создаёт вектор F = [0, 0, 0, 0] с привязкой к узлам.
    /// Порядок: [node1, node2, node3, node4].
    /// </summary>
    private SolutionItem[] CreateZeroVectorF()
        =>
        [
            new() { Node = _node1, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _node2, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _node3, Freedom = Freedom.Temperature, Value = 0.0 },
            new() { Node = _node4, Freedom = Freedom.Temperature, Value = 0.0 }
        ];

    private static DirichletCondition MakeCondition(IElementNode node, double value)
        => new()
        {
            Node = node,
            Freedom = Freedom.Temperature,
            Value = value
        };

    /// <summary>
    /// Создаёт мок IElementNode с заданным идентификатором.
    /// </summary>
    private static IElementNode CreateNodeMock(string userId)
    {
        var node = Substitute.For<IElementNode>();
        node.NodeId.Returns(userId);
        return node;
    }

    private IElementNode ResolveNode(string nodeKey)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeId == nodeKey);

        if (node is null)
            throw new ArgumentOutOfRangeException(
                nameof(nodeKey), nodeKey, "Unknown node key.");

        return node;
    }
}