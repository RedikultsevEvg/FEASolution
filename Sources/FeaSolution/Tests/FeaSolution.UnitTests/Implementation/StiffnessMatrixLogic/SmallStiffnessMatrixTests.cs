using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using Moq;
using Throws = NUnit.Framework.Throws;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
[TestOf(typeof(SmallStiffnessMatrix))]
public class SmallStiffnessMatrixTests
{
    private SmallStiffnessMatrix _matrix = null!;
    private IElementNode _nodeA = null!;
    private IElementNode _nodeB = null!;
    private IElementNode _nodeC = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeA = Mock.Of<IElementNode>();
        _nodeB = Mock.Of<IElementNode>();
        _nodeC = Mock.Of<IElementNode>();

        _matrix = new SmallStiffnessMatrix([_nodeA, _nodeB, _nodeC]);
    }

    #region Конструктор

    [Test]
    public void Constructor_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<IElementNode>? nodes = null;

        // Act
        SmallStiffnessMatrix Act() => new(nodes!);

        // Assert
        Assert.That(((Func<SmallStiffnessMatrix>?)Act)!, Throws.TypeOf<ArgumentNullException>());
    }
    
    [Test]
    public void Constructor_WithEmptyNodes_ThrowsArgumentException()
    {
        // Arrange
        var nodes = Array.Empty<IElementNode>();

        // Act
        SmallStiffnessMatrix Func() => new(nodes);

        // Assert
        Assert.That((Func<SmallStiffnessMatrix>?)Func!, Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_WithValidNodes_ExposesAllNodes()
    {
        // Arrange
        var nodes = new[] { _nodeA, _nodeB, _nodeC };

        // Act
        var matrix = new SmallStiffnessMatrix(nodes);

        // Assert
        Assert.That(matrix.Nodes, Is.EqualTo(nodes));
    }

    [Test]
    public void Constructor_WithSingleNode_Succeeds()
    {
        // Arrange & Act
        var matrix = new SmallStiffnessMatrix([_nodeA]);

        // Assert
        Assert.That(matrix.Nodes, Has.Count.EqualTo(1));
    }

    #endregion

    #region Nodes

    [Test]
    public void Nodes_PreservesInsertionOrder()
    {
        // Arrange
        var expected = new[] { _nodeA, _nodeB, _nodeC };

        // Act
        var actual = _matrix.Nodes;

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Nodes_IsIndependentOfSourceList()
    {
        // Arrange
        var source = new List<IElementNode> { _nodeA, _nodeB, _nodeC };
        var matrix = new SmallStiffnessMatrix(source);

        // Act
        source.Clear();

        // Assert
        Assert.That(matrix.Nodes, Has.Count.EqualTo(3));
    }

    [Test]
    public void Nodes_Count_MatchesConstructorArgument()
    {
        // Arrange
        var nodes = new[] { _nodeA, _nodeB };

        // Act
        var matrix = new SmallStiffnessMatrix(nodes);

        // Assert
        Assert.That(matrix.Nodes, Has.Count.EqualTo(2));
    }

    #endregion

    #region Индексатор — чтение

    [Test]
    public void Indexer_GetOnEmptyMatrix_ReturnsZero()
    {
        // Act
        var value = _matrix[_nodeA, _nodeB];

        // Assert
        Assert.That(value, Is.Zero);
    }

    [Test]
    public void Indexer_GetForPairNotWritten_ReturnsZero()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;

        // Act
        var value = _matrix[_nodeA, _nodeC];

        // Assert
        Assert.That(value, Is.Zero);
    }

    [Test]
    public void Indexer_GetForUnknownNode_ReturnsZero()
    {
        // Arrange
        var stranger = Mock.Of<IElementNode>();

        // Act
        var value = _matrix[_nodeA, stranger];

        // Assert
        Assert.That(value, Is.Zero);
    }

    [Test]
    public void Indexer_GetForBothUnknownNodes_ReturnsZero()
    {
        // Arrange
        var x = Mock.Of<IElementNode>();
        var y = Mock.Of<IElementNode>();

        // Act
        var value = _matrix[x, y];

        // Assert
        Assert.That(value, Is.Zero);
    }

    #endregion

    #region Индексатор — запись

    [Test]
    public void Indexer_Set_StoresValue()
    {
        // Arrange
        const double expected = 3.5;

        // Act
        _matrix[_nodeA, _nodeB] = expected;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(expected));
    }

    [Test]
    public void Indexer_SetSamePairTwice_OverwritesValue()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        _matrix[_nodeA, _nodeB] = 2.0;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(2.0));
    }

    [Test]
    public void Indexer_SetWithUnknownNode_DoesNothing()
    {
        // Arrange
        var stranger = Mock.Of<IElementNode>();

        // Act
        _matrix[_nodeA, stranger] = 42.0;
        _matrix[stranger, _nodeA] = 42.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, stranger], Is.Zero);
            Assert.That(_matrix[stranger, _nodeA], Is.Zero);
            Assert.That(_matrix.Nodes, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void Indexer_Set_DoesNotAddNewNodes()
    {
        // Arrange
        var stranger = Mock.Of<IElementNode>();
        var beforeCount = _matrix.Nodes.Count;

        // Act
        _matrix[_nodeA, stranger] = 1.0;

        // Assert
        Assert.That(_matrix.Nodes, Has.Count.EqualTo(beforeCount));
    }

    #endregion

    #region Симметрия

    [Test]
    public void Indexer_IsSymmetric_WhenWritingUpperTriangle()
    {
        // Arrange & Act
        _matrix[_nodeA, _nodeB] = 7.0;

        // Assert
        Assert.That(_matrix[_nodeB, _nodeA], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_IsSymmetric_WhenWritingLowerTriangle()
    {
        // Arrange & Act
        _matrix[_nodeB, _nodeA] = 7.0;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_WritingMirroredPair_OverwritesValue()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        _matrix[_nodeB, _nodeA] = 2.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(2.0));
            Assert.That(_matrix[_nodeB, _nodeA], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void Indexer_AllPairsInThreeByThree_AreSymmetric()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;
        _matrix[_nodeA, _nodeC] = 2.0;
        _matrix[_nodeB, _nodeC] = 3.0;

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeB, _nodeA], Is.EqualTo(1.0));
            Assert.That(_matrix[_nodeC, _nodeA], Is.EqualTo(2.0));
            Assert.That(_matrix[_nodeC, _nodeB], Is.EqualTo(3.0));
        });
    }

    #endregion

    #region Диагональ

    [Test]
    public void Indexer_Diagonal_StoresValue()
    {
        // Arrange
        const double expected = 4.0;

        // Act
        _matrix[_nodeA, _nodeA] = expected;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeA], Is.EqualTo(expected));
    }

    [Test]
    public void Indexer_Diagonal_IsIdempotentForMirroredAccess()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 4.0;

        // Act
        var value = _matrix[_nodeA, _nodeA];

        // Assert
        Assert.That(value, Is.EqualTo(4.0));
    }

    #endregion

    #region Обнуление (запись DefaultValue)

    [Test]
    public void Indexer_SetZero_ClearsExistingValue()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;

        // Act
        _matrix[_nodeA, _nodeB] = 0.0;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.Zero);
    }

    [Test]
    public void Indexer_SetZero_DoesNotAffectOtherCells()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;
        _matrix[_nodeB, _nodeC] = 7.0;

        // Act
        _matrix[_nodeA, _nodeB] = 0.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, _nodeB], Is.Zero);
            Assert.That(_matrix[_nodeB, _nodeC], Is.EqualTo(7.0));
        });
    }

    [Test]
    public void Indexer_SetZero_KeepsNodesRegistered()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;

        // Act
        _matrix[_nodeA, _nodeB] = 0.0;

        // Assert
        Assert.That(_matrix.Nodes, Has.Count.EqualTo(3));
    }

    #endregion

    #region NonZeroElements

    [Test]
    public void NonZeroElements_OnEmptyMatrix_IsEmpty()
    {
        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NonZeroElements_SkipsZeroCells()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;
        _matrix[_nodeB, _nodeC] = 0.0;
        _matrix[_nodeA, _nodeC] = 2.0;

        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
    }

    [Test]
    public void NonZeroElements_ReturnsOnlyDiagonal_WhenOnlyDiagonalSet()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeB, _nodeB] = 2.0;
        _matrix[_nodeC, _nodeC] = 3.0;

        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result.Select(e => e.Value), Is.EquivalentTo([1.0, 2.0, 3.0]));
        });
    }

    [Test]
    public void NonZeroElements_DoesNotYieldDuplicateSymmetricPairs()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(1));
    }

    [Test]
    public void NonZeroElements_ReturnsUpperTriangleOnly()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(6));
    }

    [Test]
    public void NonZeroElements_ReturnsNodesFromMatrixInstance()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        var result = _matrix.NonZeroElements().Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.I, Is.SameAs(_nodeA));
            Assert.That(result.J, Is.SameAs(_nodeB));
            Assert.That(result.Value, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void NonZeroElements_YieldsPairsInUpperTriangleOrder()
    {
        // Arrange: заполним все 6 ячеек верхнего треугольника
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act
        var result = _matrix.NonZeroElements().ToArray();

        // Assert: ожидаем (A,A), (A,B), (A,C), (B,B), (B,C), (C,C)
        var expected = new[]
        {
            (_nodeA, _nodeA, 1.0),
            (_nodeA, _nodeB, 2.0),
            (_nodeA, _nodeC, 3.0),
            (_nodeB, _nodeB, 4.0),
            (_nodeB, _nodeC, 5.0),
            (_nodeC, _nodeC, 6.0),
        };

        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region ReferenceEqualityComparer

    [Test]
    public void IndexOf_UsesReferenceEquality_NotEquals()
    {
        // Arrange: два мока, которые могут быть равны по Equals,
        // но различны по ссылке
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();

        var matrix = new SmallStiffnessMatrix([node1, node2]);

        // Act
        matrix[node1, node2] = 10.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix[node1, node2], Is.EqualTo(10.0));
            Assert.That(matrix[node2, node1], Is.EqualTo(10.0));
        });
    }

    #endregion

    #region Матрицы разных размеров

    [Test]
    public void OneByOneMatrix_StoresDiagonal()
    {
        // Arrange
        var matrix = new SmallStiffnessMatrix([_nodeA]);

        // Act
        matrix[_nodeA, _nodeA] = 5.0;

        // Assert
        Assert.That(matrix[_nodeA, _nodeA], Is.EqualTo(5.0));
    }

    [Test]
    public void TwoByTwoMatrix_StoresAllUpperTriangleCells()
    {
        // Arrange
        var matrix = new SmallStiffnessMatrix([_nodeA, _nodeB]);

        // Act
        matrix[_nodeA, _nodeA] = 1.0;
        matrix[_nodeA, _nodeB] = 2.0;
        matrix[_nodeB, _nodeB] = 3.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix.NonZeroElements().Count(), Is.EqualTo(3));
            Assert.That(matrix[_nodeB, _nodeA], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void SixBySixMatrix_HandlesAllCellsWithoutCollisions()
    {
        // Arrange
        var nodes = Enumerable.Range(0, 6)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();
        var matrix = new SmallStiffnessMatrix(nodes);
        var expectedCount = 0;

        // Act: 
        for (var i = 0; i < nodes.Length; i++)
            for (var j = i; j < nodes.Length; j++)
            {
                matrix[nodes[i], nodes[j]] = i * 10 + j + 1;
                expectedCount++;
            }

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix.NonZeroElements().Count(), Is.EqualTo(expectedCount));
            Assert.That(matrix[nodes[5], nodes[0]], Is.EqualTo(6));
            Assert.That(matrix[nodes[0], nodes[5]], Is.EqualTo(6));
        });
    }

    #endregion

    #region Комбинации

    [Test]
    public void FullThreeByThree_FillsUpperTriangleSymmetrically()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeB, _nodeA], Is.EqualTo(2.0));
            Assert.That(_matrix[_nodeC, _nodeA], Is.EqualTo(3.0));
            Assert.That(_matrix[_nodeC, _nodeB], Is.EqualTo(5.0));
        });
    }

    [Test]
    public void ClearingAllElements_KeepsNodesAndNonZeroElementsEmpty()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;
        _matrix[_nodeB, _nodeC] = 2.0;

        // Act
        _matrix[_nodeA, _nodeB] = 0.0;
        _matrix[_nodeB, _nodeC] = 0.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix.Nodes, Has.Count.EqualTo(3));
            Assert.That(_matrix.NonZeroElements(), Is.Empty);
        });
    }

    #endregion
}