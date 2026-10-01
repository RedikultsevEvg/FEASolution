using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixs;
using Moq;
using Throws = NUnit.Framework.Throws;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixs;

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

    #region Constructor

    [Test]
    public void Constructor_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<IElementNode>? nodes = null;

        // Act
        var act = () => new SmallStiffnessMatrix(nodes!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(() => act());
        Assert.That(ex!.ParamName, Is.EqualTo("nodes"));
    }

    [Test]
    public void Constructor_WithEmptyNodes_ThrowsFeaCommonException_WithExpectedMessage()
    {
        // Arrange
        var nodes = Array.Empty<IElementNode>();

        // Act
        var act = () => new SmallStiffnessMatrix(nodes);

        // Assert
        var ex = Assert.Throws<FeaCommonException>(() => act());
        Assert.That(ex!.Message, Is.EqualTo("Nodes cannot be empty."));
    }

    [Test]
    public void Constructor_WithThirteenNodes_ThrowsFeaCommonException_WithExpectedMessage()
    {
        // Arrange — на один узел больше задокументированного лимита
        var nodes = Enumerable.Range(0, 13)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();

        // Act
        var act = () => new SmallStiffnessMatrix(nodes);

        // Assert
        var ex = Assert.Throws<FeaCommonException>(() => act());
        Assert.That(ex!.Message,
            Is.EqualTo("SmallStiffnessMatrix supports at most 12 nodes."));
    }

    [Test]
    public void Constructor_WithThirteenDuplicateNodes_StillThrows()
    {
        // Arrange — 13 ссылок на один и тот же узел
        var nodes = Enumerable.Repeat(_nodeA, 13).ToArray();

        // Act
        var act = () => new SmallStiffnessMatrix(nodes);

        // Assert — лимит проверяется по Count, а не по количеству уникальных ссылок
        Assert.Throws<FeaCommonException>(() => act());
    }

    [Test]
    public void Constructor_WithMaxNodes_DoesNotThrow()
    {
        // Arrange — ровно 12 узлов, граница включительная
        var nodes = Enumerable.Range(0, 12)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();

        // Act
        var matrix = new SmallStiffnessMatrix(nodes);

        // Assert
        Assert.That(matrix.Nodes, Has.Count.EqualTo(12));
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

    [Test]
    public void Constructor_WithDuplicateNodes_FixesBehavior()
    {
        // Arrange — один и тот же объект дважды
        var nodes = new[] { _nodeA, _nodeA, _nodeB };

        // Act
        var matrix = new SmallStiffnessMatrix(nodes);

        // Assert — фиксируем текущее поведение: дубликаты допускаются,
        // IndexOf возвращает первый индекс.
        Assert.Multiple(() =>
        {
            Assert.That(matrix.Nodes, Has.Count.EqualTo(3));
            matrix[_nodeA, _nodeB] = 1.0;
            Assert.That(matrix[_nodeA, _nodeB], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void Constructor_WithTwelveNodes_CreatesMatrix()
    {
        // Arrange — верхняя граница дизайна
        var nodes = Enumerable.Range(0, 12)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();

        // Act
        var matrix = new SmallStiffnessMatrix(nodes);

        // Assert
        Assert.That(matrix.Nodes, Has.Count.EqualTo(12));
    }

    [Test]
    public void Constructor_CopiesSourceList_NotOnlyOnClear()
    {
        // Arrange
        var source = new List<IElementNode> { _nodeA, _nodeB, _nodeC };
        var matrix = new SmallStiffnessMatrix(source);
        var stranger = Mock.Of<IElementNode>();

        // Act — подменяем элемент в исходном списке
        source[0] = stranger;

        // Assert — матрица сохранила исходный узел
        Assert.That(matrix.Nodes[0], Is.SameAs(_nodeA));
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

    [Test]
    public void Nodes_ReturnsSameInstance_OnRepeatedCalls()
    {
        // Arrange
        // (_matrix создана в SetUp)

        // Act
        var first = _matrix.Nodes;
        var second = _matrix.Nodes;

        // Assert — без аллокаций на каждый вызов
        Assert.That(first, Is.SameAs(second));
    }

    #endregion

    #region DefaultValue

    [Test]
    public void DefaultValue_IsZero()
    {
        // Arrange
        // (_matrix создана в SetUp)

        // Act
        var defaultValue = _matrix.DefaultValue;

        // Assert
        Assert.That(defaultValue, Is.Zero);
    }

    #endregion

    #region Indexer.Get

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

    [Test]
    public void Indexer_GetDiagonalOnEmptyMatrix_ReturnsZero()
    {
        // Arrange
        // (_matrix создана в SetUp)

        // Act
        var value = _matrix[_nodeA, _nodeA];

        // Assert
        Assert.That(value, Is.Zero);
    }

    [Test]
    public void Indexer_Get_IsSymmetric_WhenWritingUpperTriangle()
    {
        // Arrange & Act
        _matrix[_nodeA, _nodeB] = 7.0;

        // Assert
        Assert.That(_matrix[_nodeB, _nodeA], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_Get_IsSymmetric_WhenWritingLowerTriangle()
    {
        // Arrange & Act
        _matrix[_nodeB, _nodeA] = 7.0;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_Get_AllPairsInThreeByThree_AreSymmetric()
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

    [Test]
    public void Indexer_Get_Diagonal_IsIdempotentForMirroredAccess()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 4.0;

        // Act
        var value = _matrix[_nodeA, _nodeA];

        // Assert
        Assert.That(value, Is.EqualTo(4.0));
    }

    #endregion

    #region Indexer.Set

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

    [Test]
    public void Indexer_SetNegativeValue_StoresValue()
    {
        // Arrange
        const double expected = -3.5;

        // Act
        _matrix[_nodeA, _nodeB] = expected;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(expected));
    }

    [Test]
    public void Indexer_SetNegativeZero_RemovesCell()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;

        // Act — в C# -0.0 == 0.0, значит ячейка должна обнулиться
        _matrix[_nodeA, _nodeB] = -0.0;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeB], Is.Zero);
    }

    [Test]
    public void Indexer_SetTinyNonZeroValue_StoresValue()
    {
        // Arrange
        const double tiny = 1e-300;

        // Act
        _matrix[_nodeA, _nodeB] = tiny;

        // Assert — сравнение с DefaultValue точное, не по epsilon
        Assert.That(_matrix[_nodeA, _nodeB], Is.EqualTo(tiny));
    }

    [Test]
    public void Indexer_Set_WritingMirroredPair_OverwritesValue()
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
    public void Indexer_Set_Diagonal_StoresValue()
    {
        // Arrange
        const double expected = 4.0;

        // Act
        _matrix[_nodeA, _nodeA] = expected;

        // Assert
        Assert.That(_matrix[_nodeA, _nodeA], Is.EqualTo(expected));
    }

    [Test]
    public void Indexer_Set_FullThreeByThree_FillsUpperTriangleSymmetrically()
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

    #endregion

    #region Indexer.SetDefault

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

    [Test]
    public void Indexer_SetZeroOnAlreadyZero_DoesNothing()
    {
        // Arrange — ячейка уже нулевая

        // Act
        _matrix[_nodeA, _nodeB] = 0.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, _nodeB], Is.Zero);
            Assert.That(_matrix.Nodes, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void Indexer_SetZeroWithUnknownNode_DoesNothing()
    {
        // Arrange
        var stranger = Mock.Of<IElementNode>();

        // Act
        _matrix[_nodeA, stranger] = 0.0;
        _matrix[stranger, _nodeA] = 0.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, stranger], Is.Zero);
            Assert.That(_matrix[stranger, _nodeA], Is.Zero);
            Assert.That(_matrix.Nodes, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void Indexer_SetZeroThroughMirroredPair_ClearsCell()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 5.0;

        // Act
        _matrix[_nodeB, _nodeA] = 0.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_nodeA, _nodeB], Is.Zero);
            Assert.That(_matrix[_nodeB, _nodeA], Is.Zero);
        });
    }

    [Test]
    public void Indexer_SetZero_ClearingAllElements_KeepsNodesAndNonZeroMatrixValuesEmpty()
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
            Assert.That(_matrix.NonZeroMatrixValues(), Is.Empty);
        });
    }

    #endregion

    #region NonZeroMatrixValues

    [Test]
    public void NonZeroMatrixValues_OnEmptyMatrix_IsEmpty()
    {
        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NonZeroMatrixValues_SkipsZeroCells()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;
        _matrix[_nodeB, _nodeC] = 0.0;
        _matrix[_nodeA, _nodeC] = 2.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
    }

    [Test]
    public void NonZeroMatrixValues_ReturnsOnlyDiagonal_WhenOnlyDiagonalSet()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeB, _nodeB] = 2.0;
        _matrix[_nodeC, _nodeC] = 3.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result.Select(e => e.Value), Is.EquivalentTo([1.0, 2.0, 3.0]));
        });
    }

    [Test]
    public void NonZeroMatrixValues_DoesNotYieldDuplicateSymmetricPairs()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(1));
    }

    [Test]
    public void NonZeroMatrixValues_ReturnsUpperTriangleOnly()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(6));
    }

    [Test]
    public void NonZeroMatrixValues_ReturnsNodesFromMatrixInstance()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.I, Is.SameAs(_nodeA));
            Assert.That(result.J, Is.SameAs(_nodeB));
            Assert.That(result.Value, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void NonZeroMatrixValues_YieldsPairsInUpperTriangleOrder()
    {
        // Arrange: заполним все 6 ячеек верхнего треугольника
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToArray();

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

    [Test]
    public void NonZeroMatrixValues_IsLazy_ReflectsLaterChanges()
    {
        // Arrange
        _matrix[_nodeA, _nodeB] = 1.0;

        // Act
        var enumerable = _matrix.NonZeroMatrixValues();
        _matrix[_nodeB, _nodeC] = 2.0;

        // Assert — при перечислении видим оба значения
        Assert.That(enumerable.Count(), Is.EqualTo(2));
    }

    [Test]
    public void NonZeroMatrixValues_PreservesRowMajorOrder_AfterPartialClearing()
    {
        // Arrange
        _matrix[_nodeA, _nodeA] = 1.0;
        _matrix[_nodeA, _nodeB] = 2.0;
        _matrix[_nodeA, _nodeC] = 3.0;
        _matrix[_nodeB, _nodeB] = 4.0;
        _matrix[_nodeB, _nodeC] = 5.0;
        _matrix[_nodeC, _nodeC] = 6.0;

        // Act — обнуляем середину
        _matrix[_nodeA, _nodeB] = 0.0;
        _matrix[_nodeB, _nodeC] = 0.0;

        // Assert — порядок оставшихся: (A,A), (A,C), (B,B), (C,C)
        var result = _matrix.NonZeroMatrixValues().ToArray();
        Assert.That(result.Select(e => (e.I, e.J, e.Value)), Is.EqualTo(new[]
        {
            (_nodeA, _nodeA, 1.0),
            (_nodeA, _nodeC, 3.0),
            (_nodeB, _nodeB, 4.0),
            (_nodeC, _nodeC, 6.0),
        }));
    }

    #endregion

    #region IndexOf

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

    [Test]
    public void IndexOf_IgnoresEquals_WhenNodesAreEqualByValue()
    {
        // Arrange — моки, у которых Equals настроен возвращать true
        var node1 = new Mock<IElementNode>();
        var node2 = new Mock<IElementNode>();
        node1.Setup(x => x.Equals(node2.Object)).Returns(true);
        node2.Setup(x => x.Equals(node1.Object)).Returns(true);

        var matrix = new SmallStiffnessMatrix([node1.Object]);

        // Act — node2 не находится, несмотря на Equals == true
        matrix[node2.Object, node2.Object] = 5.0;

        // Assert — запись проигнорирована, потому что IndexOf вернул -1
        Assert.That(matrix[node2.Object, node2.Object], Is.Zero);
    }

    [Test]
    public void IndexOf_ReturnsFirstMatch_WhenDuplicateNodes()
    {
        // Arrange — один и тот же объект дважды в разных позициях
        var nodes = new[] { _nodeA, _nodeB, _nodeA };
        var matrix = new SmallStiffnessMatrix(nodes);

        // Act — запись идёт по первому вхождению _nodeA (индекс 0)
        matrix[_nodeA, _nodeB] = 1.0;

        // Assert — читается через то же самое первое вхождение
        Assert.That(matrix[_nodeA, _nodeB], Is.EqualTo(1.0));
    }

    #endregion

    #region PackIndex

    [Test]
    public void PackIndex_IsInvolutive_ForAllPairs_InMaxSizeMatrix()
    {
        // Arrange
        var nodes = Enumerable.Range(0, 12)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();
        var matrix = new SmallStiffnessMatrix(nodes);

        // Act & Assert — пишем в (i,j), читаем из (j,i) для всех пар
        for (var i = 0; i < nodes.Length; i++)
            for (var j = i; j < nodes.Length; j++)
            {
                var value = i * 100 + j + 1;
                matrix[nodes[i], nodes[j]] = value;
                Assert.That(matrix[nodes[j], nodes[i]], Is.EqualTo(value),
                    $"Symmetry broken at ({i},{j}).");
            }
    }

    [Test]
    public void PackIndex_ProducesUniqueIndices_ForAllUpperTrianglePairs()
    {
        // Arrange
        var nodes = Enumerable.Range(0, 12)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();
        var matrix = new SmallStiffnessMatrix(nodes);
        var n = nodes.Length;
        var expectedCellCount = n * (n + 1) / 2;

        // Act — заполняем все ячейки верхнего треугольника
        for (var i = 0; i < n; i++)
            for (var j = i; j < n; j++)
                matrix[nodes[i], nodes[j]] = i * 100 + j + 1;

        // Assert — ровно n(n+1)/2 ненулевых ячеек, никаких коллизий
        Assert.That(matrix.NonZeroMatrixValues().Count(), Is.EqualTo(expectedCellCount));
    }

    #endregion

    #region AddValue

    [Test]
    public void AddValue_ThrowsNotImplementedException()
    {
        // Arrange
        // (_matrix создана в SetUp)

        // Act
        var act = () => _matrix.AddValue(_nodeA, _nodeB, 1.0);

        // Assert
        Assert.Throws<NotImplementedException>(() => act());
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
            Assert.That(matrix.NonZeroMatrixValues().Count(), Is.EqualTo(3));
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
            Assert.That(matrix.NonZeroMatrixValues().Count(), Is.EqualTo(expectedCount));
            Assert.That(matrix[nodes[5], nodes[0]], Is.EqualTo(6));
            Assert.That(matrix[nodes[0], nodes[5]], Is.EqualTo(6));
        });
    }

    [Test]
    public void TwelveByTwelveMatrix_HandlesAllUpperTriangleCells()
    {
        // Arrange
        var nodes = Enumerable.Range(0, 12)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();
        var matrix = new SmallStiffnessMatrix(nodes);
        var n = nodes.Length;
        var expectedCount = n * (n + 1) / 2;

        // Act
        for (var i = 0; i < n; i++)
            for (var j = i; j < n; j++)
                matrix[nodes[i], nodes[j]] = i * 100 + j + 1;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(matrix.NonZeroMatrixValues().Count(), Is.EqualTo(expectedCount));
            Assert.That(matrix[nodes[11], nodes[0]], Is.EqualTo(12));
            Assert.That(matrix[nodes[0], nodes[11]], Is.EqualTo(12));
        });
    }

    #endregion
}