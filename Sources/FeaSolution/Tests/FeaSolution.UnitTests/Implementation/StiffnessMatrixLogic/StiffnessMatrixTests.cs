using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using Moq;

namespace FeaSolution.UnitTests.Implementation.StiffnessMatrixLogic;

[TestFixture]
[TestOf(typeof(StiffnessMatrix))]
public class StiffnessMatrixTests
{
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

    // ---------- Инициализация ----------

    [Test]
    public void NewMatrix_HasZeroNodeCount()
    {
        Assert.That(_matrix.NodeCount, Is.Zero);
    }

    [Test]
    public void NewMatrix_DefaultValueIsZero()
    {
        Assert.That(_matrix.DefaultValue, Is.EqualTo(0.0));
    }

    // ---------- Чтение до записи ----------

    [Test]
    public void Indexer_GetOnEmptyMatrix_ReturnsDefaultValue()
    {
        Assert.That(_matrix[_a, _b], Is.EqualTo(_matrix.DefaultValue));
    }

    [Test]
    public void Indexer_GetAfterWritingDifferentPair_ReturnsDefaultValue()
    {
        _matrix[_a, _b] = 5.0;

        Assert.That(_matrix[_a, _c], Is.EqualTo(0.0));
        Assert.That(_matrix[_b, _c], Is.EqualTo(0.0));
    }

    [Test]
    public void Indexer_GetForUnknownNodes_ReturnsDefaultValue_AndDoesNotRegister()
    {
        var result = _matrix[_a, _b];

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0.0));
            Assert.That(_matrix.NodeCount, Is.Zero);
        });
    }

    // ---------- Запись ----------

    [Test]
    public void Indexer_Set_StoresValue()
    {
        _matrix[_a, _b] = 3.5;

        Assert.That(_matrix[_a, _b], Is.EqualTo(3.5));
    }

    [Test]
    public void Indexer_Set_RegistersBothNodes()
    {
        _matrix[_a, _b] = 1.0;

        Assert.That(_matrix.NodeCount, Is.EqualTo(2));
    }

    [Test]
    public void Indexer_SetSamePairTwice_OverwritesValue()
    {
        _matrix[_a, _b] = 1.0;
        _matrix[_a, _b] = 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(2.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Indexer_SetDifferentPairs_IncrementsNodeAndElementCount()
    {
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(3));
        });
    }

    // ---------- Симметрия ----------

    [Test]
    public void Indexer_IsSymmetric_WhenWritingOneDirection()
    {
        _matrix[_a, _b] = 7.0;

        Assert.That(_matrix[_b, _a], Is.EqualTo(7.0));
    }

    [Test]
    public void Indexer_IsSymmetric_WhenWritingBothDirections()
    {
        _matrix[_a, _b] = 7.0;
        _matrix[_b, _a] = 7.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(7.0));
        });
    }

    [Test]
    public void Indexer_WritingSecondDirectionOverwritesFirst()
    {
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _a] = 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(2.0));
            Assert.That(_matrix[_b, _a], Is.EqualTo(2.0));
        });
    }

    // ---------- Диагональ ----------

    [Test]
    public void Indexer_Diagonal_StoresValue()
    {
        _matrix[_a, _a] = 4.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _a], Is.EqualTo(4.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(1));
        });
    }

    // ---------- Удаление через запись DefaultValue ----------

    [Test]
    public void Indexer_SetDefault_RemovesElement()
    {
        _matrix[_a, _b] = 5.0;
        _matrix[_a, _b] = _matrix.DefaultValue;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _b], Is.EqualTo(0.0));
        });
    }

    [Test]
    public void Indexer_SetDefault_KeepsNodesRegistered()
    {
        _matrix[_a, _b] = 5.0;
        _matrix[_a, _b] = 0.0;

        Assert.That(_matrix.NodeCount, Is.EqualTo(2),
            "Узлы не должны удаляться при обнулении ячейки.");
    }

    [Test]
    public void Indexer_SetDefaultOnEmptyPair_DoesNotRegisterNodes()
    {
        _matrix[_a, _b] = 0.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.Zero);
        });
    }

    [Test]
    public void Indexer_SetDefaultOnOneKnownOneUnknownNode_DoesNothing()
    {
        _matrix[_a, _b] = 1.0;   // регистрирует A и B
        _matrix[_a, _c] = 0.0;   // C ещё не зарегистрирован

        Assert.Multiple(() =>
        {
            Assert.That(_matrix[_a, _c], Is.EqualTo(0.0));
            Assert.That(_matrix.NodeCount, Is.EqualTo(2),
                "C не должен зарегистрироваться при записи нуля.");
        });
    }

    // ---------- ReferenceEqualityComparer ----------

    [Test]
    public void DifferentMockInstances_AreTreatedAsDifferentNodes()
    {
        var node1 = Mock.Of<IElementNode>();
        var node2 = Mock.Of<IElementNode>();   // другая ссылка

        _matrix[node1, node1] = 1.0;
        _matrix[node2, node2] = 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(2));
            Assert.That(_matrix[node1, node1], Is.EqualTo(1.0));
            Assert.That(_matrix[node2, node2], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void SameMockInstance_IsTreatedAsSameNode()
    {
        var node = Mock.Of<IElementNode>();

        _matrix[node, _a] = 1.0;
        _matrix[node, _b] = 2.0;

        Assert.That(_matrix.NodeCount, Is.EqualTo(3),
            "node + A + B — три уникальных узла.");
    }

    // ---------- Комбинации ----------

    [Test]
    public void FullThreeByThree_FillsUpperTriangle()
    {
        _matrix[_a, _a] = 1.0;
        _matrix[_a, _b] = 2.0;
        _matrix[_a, _c] = 3.0;
        _matrix[_b, _b] = 4.0;
        _matrix[_b, _c] = 5.0;
        _matrix[_c, _c] = 6.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(3));

            Assert.That(_matrix[_b, _a], Is.EqualTo(2.0));
            Assert.That(_matrix[_c, _a], Is.EqualTo(3.0));
            Assert.That(_matrix[_c, _b], Is.EqualTo(5.0));
        });
    }

    [Test]
    public void ClearingAllElements_KeepsNodeCount()
    {
        _matrix[_a, _b] = 1.0;
        _matrix[_b, _c] = 2.0;

        _matrix[_a, _b] = 0.0;
        _matrix[_b, _c] = 0.0;

        Assert.Multiple(() =>
        {
            Assert.That(_matrix.NodeCount, Is.EqualTo(3),
                "Узлы остаются зарегистрированными.");
        });
    }
}