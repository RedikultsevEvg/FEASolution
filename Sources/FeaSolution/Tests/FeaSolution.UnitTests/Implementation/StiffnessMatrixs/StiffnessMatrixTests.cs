using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixs;
using Moq;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixs;

[TestFixture]
[TestOf(typeof(StiffnessMatrix))]
public class StiffnessMatrixTests
{
    private const double Tolerance = 1e-12;

    private StiffnessMatrix _matrix = null!;
    private IElementNode _a = null!;
    private IElementNode _b = null!;
    private IElementNode _c = null!;

    [SetUp]
    public void SetUp()
    {
        _matrix = new StiffnessMatrix();

        _a = Mock.Of<IElementNode>();
        _b = Mock.Of<IElementNode>();
        _c = Mock.Of<IElementNode>();
    }

    #region NodeCount

    [Test]
    public void NodeCount_OnNewMatrix_IsZero()
    {
        // Arrange
        // (matrix создана в SetUp)

        // Act
        var nodeCount = _matrix.NodeCount;

        // Assert
        Assert.That(nodeCount, Is.Zero);
    }

    [Test]
    public void NodeCount_AfterClearingAllElements_IsPreserved()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;

        // Act
        _matrix[_a, _b] = 0.0;
        _matrix[_b, _c] = 0.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(3),
            "Узлы остаются зарегистрированными.");
    }

    #endregion

    #region DefaultValue

    [Test]
    public void DefaultValue_OnNewMatrix_IsZero()
    {
        // Arrange
        // (matrix создана в SetUp)

        // Act
        var defaultValue = _matrix.DefaultValue;

        // Assert
        Assert.That(defaultValue, Is.EqualTo(0.0));
    }

    #endregion

    #region Nodes

    [Test]
    public void Nodes_AreEmpty_OnNewMatrix()
    {
        // Arrange
        // (matrix пуста)

        // Act
        var nodes = _matrix.Nodes;

        // Assert
        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public void Nodes_PreserveRegistrationOrder()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;

        // Act
        var nodes = _matrix.Nodes;

        // Assert
        Assert.That(nodes, Is.EqualTo(new[] { _a, _b, _c }));
    }

    [Test]
    public void Nodes_ContainsNoDuplicates_WhenNodeReused()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;
        _matrix[_a, _c] = 2.0;
        _matrix[_b, _c] = 3.0;

        // Act
        var nodes = _matrix.Nodes;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Count.EqualTo(3));
            Assert.That(nodes.Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(3));
        });
    }

    [Test]
    public void Nodes_CountMatchesNodeCount()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;

        // Act
        var nodes = _matrix.Nodes;

        // Assert
        Assert.That(nodes.Count, Is.EqualTo(_matrix.NodeCount));
    }

    #endregion

    #region Indexer.Get

    [Test]
    public void Indexer_GetOnEmptyMatrix_ReturnsDefaultValue()
    {
        // Arrange
        // (matrix пуста)

        // Act
        var result = _matrix[_a, _b];

        // Assert
        Assert.That(result, Is.EqualTo(_matrix.DefaultValue));
    }

    [Test]
    public void Indexer_GetAfterWritingDifferentPair_ReturnsDefaultValue()
    {
        // Arrange
        _matrix[_a, _b] = 5.0;

        // Act
        var resultAc = _matrix[_a, _c];
        var resultBc = _matrix[_b, _c];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultAc, Is.EqualTo(0.0));
            Assert.That(resultBc, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void Indexer_GetForUnknownNodes_ReturnsDefaultValue_AndDoesNotRegister()
    {
        // Arrange
        // (matrix пуста)

        // Act
        var result = _matrix[_a, _b];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0.0));
            Assert.That(_matrix.NodeCount, Is.Zero);
        });
    }

    [Test]
    public void Indexer_Get_IsSymmetric_WhenWritingOneDirection()
    {
        // Arrange
        _matrix[_a, _b] = 7.0;

        // Act
        var result = _matrix[_b, _a];

        // Assert
        Assert.That(result, Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_Get_WhenOnlyFirstNodeKnown_ReturnsDefault()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        var result = _matrix[_a, _c];

        // Assert
        Assert.That(result, Is.EqualTo(_matrix.DefaultValue));
    }

    [Test]
    public void Indexer_Get_WhenOnlySecondNodeKnown_ReturnsDefault()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        var result = _matrix[_c, _a];

        // Assert
        Assert.That(result, Is.EqualTo(_matrix.DefaultValue));
    }

    #endregion

    #region Indexer.Set

    [Test]
    public void Indexer_Set_StoresValue()
    {
        // Arrange
        const double value = 3.5;

        // Act
        _matrix[_a, _b] = value;

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(value));
    }

    [Test]
    public void Indexer_Set_RegistersBothNodes()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix[_a, _b] = 1.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(2));
    }

    [Test]
    public void Indexer_SetSamePairTwice_OverwritesValue()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        _matrix[_a, _b] = 2.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(2.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Indexer_SetDifferentPairs_IncrementsNodeCount()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        _matrix[_b, _c] = 2.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(3));
    }

    [Test]
    public void Indexer_Set_IsSymmetric_WhenWritingBothDirections()
    {
        // Arrange
        _matrix[_a, _b] = 7.0;

        // Act
        _matrix[_b, _a] = 7.0;

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_Set_WritingSecondDirectionOverwritesFirst()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        _matrix[_b, _a] = 2.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(2.0));
            Assert.That(_matrix[_b, _a], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void Indexer_Set_Diagonal_StoresValue()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix[_a, _a] = 4.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _a], Is.EqualTo(4.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Indexer_FullThreeByThree_FillsUpperTriangle()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix[_a, _a] = 1.0;
        _matrix[_a, _b] = 2.0;
        _matrix[_a, _c] = 3.0;
        _matrix[_b, _b] = 4.0;
        _matrix[_b, _c] = 5.0;
        _matrix[_c, _c] = 6.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(3));

            Assert.That(_matrix[_b, _a], Is.EqualTo(2.0));
            Assert.That(_matrix[_c, _a], Is.EqualTo(3.0));
            Assert.That(_matrix[_c, _b], Is.EqualTo(5.0));
        });
    }

    [Test]
    public void Indexer_IsOrderIndependent_WhenUsingLargeIndices()
    {
        // Arrange — создаём достаточно узлов, чтобы индексы были ощутимыми,
        // и проверяем, что (i,j) и (j,i) дают одну ячейку.
        var nodes = Enumerable.Range(0, 10)
            .Select(_ => Mock.Of<IElementNode>())
            .ToArray();

        // Act
        _matrix[nodes[3], nodes[7]] = 1.0;
        _matrix[nodes[7], nodes[3]] = 2.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[nodes[3], nodes[7]], Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(_matrix.NonZeroMatrixValues().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void Indexer_Set_AfterAddValue_OverwritesAccumulatedSum()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 1.0);
        _matrix.AddValue(_a, _b, 2.0);

        // Act
        _matrix[_a, _b] = 10.0;

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(10.0).Within(Tolerance));
    }

    #endregion

    #region Indexer.SetDefault

    [Test]
    public void Indexer_SetDefault_RemovesElement()
    {
        // Arrange
        _matrix[_a, _b] = 5.0;

        // Act
        _matrix[_a, _b] = _matrix.DefaultValue;

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(0.0));
    }

    [Test]
    public void Indexer_SetDefault_KeepsNodesRegistered()
    {
        // Arrange
        _matrix[_a, _b] = 5.0;

        // Act
        _matrix[_a, _b] = 0.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(2),
            "Узлы не должны удаляться при обнулении ячейки.");
    }

    [Test]
    public void Indexer_SetDefaultOnEmptyPair_DoesNotRegisterNodes()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix[_a, _b] = 0.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.Zero);
    }

    [Test]
    public void Indexer_SetDefaultOnOneKnownOneUnknownNode_DoesNothing()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;   // регистрирует A и B

        // Act
        _matrix[_a, _c] = 0.0;   // C ещё не зарегистрирован

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _c], Is.EqualTo(0.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2),
                "C не должен зарегистрироваться при записи нуля.");
        });
    }

    #endregion

    #region Indexer.ReferenceEquality

    [Test]
    public void Indexer_DifferentMockInstances_AreTreatedAsDifferentNodes()
    {
        // Arrange
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();   // другая ссылка

        // Act
        _matrix[node1, node1] = 1.0;
        _matrix[node2, node2] = 2.0;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
            Assert.That(_matrix[node1, node1], Is.EqualTo(1.0));
            Assert.That(_matrix[node2, node2], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void Indexer_SameMockInstance_IsTreatedAsSameNode()
    {
        // Arrange
        var node = Mock.Of<IElementNode>();

        // Act
        _matrix[node, _a] = 1.0;
        _matrix[node, _b] = 2.0;

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(3),
            "node + A + B — три уникальных узла.");
    }

    #endregion

    #region AddValue

    [Test]
    public void AddValue_OnEmptyMatrix_StoresValue()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix.AddValue(_a, _b, 5.0);

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(5.0).Within(Tolerance));
    }

    [Test]
    public void AddValue_RegistersBothNodes()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix.AddValue(_a, _b, 1.0);

        // Assert
        Assert.That(_matrix.NodeCount, Is.EqualTo(2));
    }

    [Test]
    public void AddValue_SamePairTwice_SumsValues()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 2.0);

        // Act
        _matrix.AddValue(_a, _b, 3.0);

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(5.0).Within(Tolerance));
    }

    [Test]
    public void AddValue_OppositeDirections_SumsIntoOneCell()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 2.0);

        // Act
        _matrix.AddValue(_b, _a, 3.0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(_matrix[_b, _a], Is.EqualTo(5.0).Within(Tolerance));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddValue_SummingToZero_RemovesCellButKeepsNodes()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 5.0);

        // Act
        _matrix.AddValue(_a, _b, -5.0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2),
                "Узлы должны остаться зарегистрированными.");
        });
    }

    [Test]
    public void AddValue_SummingToZero_RemovesElementFromNonZeroElements()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 5.0);
        _matrix.AddValue(_a, _b, -5.0);
        _matrix.AddValue(_b, _c, 1.0);

        // Act
        var nonZero = _matrix.NonZeroMatrixValues().ToList();

        // Assert
        Assert.That(nonZero, Has.Count.EqualTo(1));
        Assert.That(nonZero[0].I, Is.SameAs(_b));
        Assert.That(nonZero[0].J, Is.SameAs(_c));
    }

    [Test]
    public void AddValue_WithZero_OnEmptyMatrix_DoesNotRegisterNodes()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix.AddValue(_a, _b, 0.0);

        // Assert
        Assert.That(_matrix.NodeCount, Is.Zero);
    }

    [Test]
    public void AddValue_WithZero_DoesNotCreateCell()
    {
        // Arrange
        // (matrix пуста)

        // Act
        _matrix.AddValue(_a, _b, 0.0);

        // Assert
        Assert.That(_matrix.NonZeroMatrixValues(), Is.Empty);
    }

    [Test]
    public void AddValue_WithZero_AfterNonZeroWrite_DoesNotChangeValue()
    {
        // Arrange
        _matrix.AddValue(_a, _b, 4.0);

        // Act
        _matrix.AddValue(_a, _b, 0.0);

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(4.0).Within(Tolerance));
    }

    [Test]
    public void AddValue_AfterSetToZero_ReusesExistingNodes()
    {
        // Arrange
        _matrix[_a, _b] = 5.0;
        _matrix[_a, _b] = 0.0;            // узел A и B остаются зарегистрированными

        // Act
        _matrix.AddValue(_a, _b, 7.0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(7.0).Within(Tolerance));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddValue_ToUnknownNodes_RegistersThem()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;            // регистрирует A и B

        // Act
        _matrix.AddValue(_a, _c, 2.0);    // C ещё не зарегистрирован

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(3));
            Assert.That(_matrix[_a, _c], Is.EqualTo(2.0).Within(Tolerance));
        });
    }

    [Test]
    public void AddValue_AccumulatesAcrossMultipleCalls_ForAssemblyLikeUsage()
    {
        // Arrange — эмулируем вклад трёх элементов в одну ячейку (a,b)
        const double k1 = 1.5, k2 = -0.25, k3 = 2.75;

        // Act
        _matrix.AddValue(_a, _b, k1);
        _matrix.AddValue(_a, _b, k2);
        _matrix.AddValue(_b, _a, k3);

        // Assert
        var expected = k1 + k2 + k3;
        Assert.That(_matrix[_a, _b], Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void AddValue_AfterSet_AddsToExistingValue()
    {
        // Arrange
        _matrix[_a, _b] = 10.0;

        // Act
        _matrix.AddValue(_a, _b, 5.0);

        // Assert
        Assert.That(_matrix[_a, _b], Is.EqualTo(15.0).Within(Tolerance));
    }

    #endregion

    #region NonZeroMatrixValues

    [Test]
    public void NonZeroElements_OnEmptyMatrix_IsEmpty()
    {
        // Arrange
        // (matrix пуста)

        // Act
        var result = _matrix.NonZeroMatrixValues();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NonZeroElements_ReturnsOnlyNonZeroCells()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;
        _matrix[_a, _c] = 3.0;
        _matrix[_a, _c] = 0.0;            // удалено

        // Act
        var result = _matrix.NonZeroMatrixValues().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void NonZeroElements_ReturnsValuesMatchingIndexerReads()
    {
        // Arrange
        _matrix[_a, _b] = 1.5;
        _matrix[_b, _c] = -2.25;
        _matrix[_a, _a] = 3.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToList();

        // Assert
        foreach (var (i, j, value) in result)
            Assert.That(_matrix[i, j], Is.EqualTo(value).Within(Tolerance));
    }

    [Test]
    public void NonZeroElements_IncludesDiagonalElements()
    {
        // Arrange
        _matrix[_a, _a] = 4.0;
        _matrix[_b, _b] = 5.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(e => ReferenceEquals(e.I, e.J)), Is.True);
    }

    [Test]
    public void NonZeroElements_ReturnsOnlyOneTrianglePerSymmetricCell()
    {
        // Arrange — пишем (a,b) и (b,a), это одна и та же ячейка
        _matrix[_a, _b] = 1.0;

        // Act
        var result = _matrix.NonZeroMatrixValues().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1),
            "Симметричная ячейка должна возвращаться один раз (верхний треугольник).");

        var (i, j, _) = result[0];
        var indexI = _matrix.Nodes.ToArray().IndexOf(i);
        var indexJ = _matrix.Nodes.ToArray().IndexOf(j);
        Assert.That(indexI, Is.LessThanOrEqualTo(indexJ),
            "Ожидается i <= j по индексу регистрации.");
    }

    [Test]
    public void NonZeroElements_PreservesReferenceToOriginalNodes()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        var (i, j, _) = _matrix.NonZeroMatrixValues().Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(i, Is.SameAs(_a));
            Assert.That(j, Is.SameAs(_b));
        });
    }

    [Test]
    public void NonZeroElements_IsLazy_EnumeratesOnlyOnDemand()
    {
        // Arrange
        _matrix[_a, _b] = 1.0;

        // Act
        var enumerable = _matrix.NonZeroMatrixValues();
        _matrix[_b, _c] = 2.0;            // добавляем до перечисления

        // Assert
        Assert.That(enumerable.Count(), Is.EqualTo(2));
    }

    #endregion

}