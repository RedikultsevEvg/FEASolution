using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ElementNodes;
using Moq;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(NodeMerger))]
public class NodeMergerTests
{
    [Test]
    public void MergeEqualNodes_ThreeTrianglesWithSharedCenter_MergesSharedNodesAcrossAdjacentElements()
    {
        // Arrange
        // Топология:
        //        A
        //       /  \
        //      / T1 \
        //     /      \
        //    B ------ C
        //     \      / \     
        //      \ T2 /   \ 
        //       \  /  T3 \  
        //        D  ----  F
        //
        // Треугольники:
        //   T1 = (A, B, C)
        //   T2 = (B, D, C)
        //   T3 = (D, F, C)
        //
        // Общие узлы:
        //   C — общий для всех трёх треугольников;
        //   B — общий для T1 и T2;
        //   D — общий для T2 и T3.

        // Уникальные экземпляры для каждого треугольника (имитация независимых
        // импортированных мешей), но с совпадающими координатами там, где
        // узлы должны слиться.
        var nodeAt1StaySingle = CreateNode(0.0, 1.0, 0.0);
        var nodeBt1ToRemove = CreateNode(-1.0, 0.0, 0.0);
        var nodeBt2ToShare = CreateNode(-1.0, 0.0, 0.0, "node B-T2");
        var nodeCt1ToRemove = CreateNode(0.0, 0.0, 0.0);
        var nodeDt2ToRemove = CreateNode(1.0, 0.0, 0.0);
        var nodeCt2ToRemove = CreateNode(0.0, 0.0, 0.0);
        var nodeDt3ToShare = CreateNode(1.0, 0.0, 0.0, "node D-T3");
        var nodeFt3StaySingle = CreateNode(0.0, -1.0, 0.0);
        var nodeCt3ToShare = CreateNode(0.0, 0.0, 0.0, "node C-T3");

        var triangleT1 = CreateElement(nodeAt1StaySingle, nodeBt1ToRemove, nodeCt1ToRemove);
        var triangleT2 = CreateElement(nodeBt2ToShare, nodeDt2ToRemove, nodeCt2ToRemove);
        var triangleT3 = CreateElement(nodeDt3ToShare, nodeFt3StaySingle, nodeCt3ToShare);

        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([triangleT1, triangleT2, triangleT3], delta);

        // Assert 
        Assert.That(triangleT1.Nodes.Count, Is.EqualTo(3));
        Assert.That(triangleT2.Nodes.Count, Is.EqualTo(3));
        Assert.That(triangleT3.Nodes.Count, Is.EqualTo(3));

        Assert.That(triangleT1.Nodes.ElementAt(0), Is.SameAs(nodeAt1StaySingle));
        Assert.That(triangleT1.Nodes.ElementAt(1), Is.SameAs(nodeBt2ToShare));
        Assert.That(triangleT1.Nodes.ElementAt(1).NodeId, Is.EqualTo("node B-T2"));
        Assert.That(triangleT1.Nodes.ElementAt(2), Is.SameAs(nodeCt3ToShare));
        Assert.That(triangleT1.Nodes.ElementAt(2).NodeId, Is.EqualTo("node C-T3"));

        Assert.That(triangleT2.Nodes.ElementAt(0), Is.SameAs(nodeBt2ToShare));
        Assert.That(triangleT2.Nodes.ElementAt(0).NodeId, Is.EqualTo("node B-T2"));
        Assert.That(triangleT2.Nodes.ElementAt(1), Is.SameAs(nodeDt3ToShare));
        Assert.That(triangleT2.Nodes.ElementAt(2), Is.SameAs(nodeCt3ToShare));
        Assert.That(triangleT2.Nodes.ElementAt(2).NodeId, Is.EqualTo("node C-T3"));

        Assert.That(triangleT3.Nodes.ElementAt(0), Is.SameAs(nodeDt3ToShare));
        Assert.That(triangleT3.Nodes.ElementAt(0).NodeId, Is.EqualTo("node D-T3")); 
        Assert.That(triangleT3.Nodes.ElementAt(1), Is.SameAs(nodeFt3StaySingle));
        Assert.That(triangleT3.Nodes.ElementAt(2), Is.SameAs(nodeCt3ToShare));
        Assert.That(triangleT3.Nodes.ElementAt(2).NodeId, Is.EqualTo("node C-T3"));
    }

    [Test]
    public void MergeEqualNodes_NullElements_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IFiniteElement> elements = null!;
        const double delta = 0.0;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => NodeMerger.MergeEqualNodes(elements, delta));
    }

    [Test]
    public void MergeEqualNodes_NegativeDelta_ThrowsFeaModelBuilderException()
    {
        // Arrange
        var elements = new[] { CreateElement(CreateNode(0, 0, 0)) };
        const double delta = -0.001;

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => NodeMerger.MergeEqualNodes(elements, delta));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The merge delta should be non-negative."));
    }

    [Test]
    public void MergeEqualNodes_EmptyElements_ThrowsFeaModelBuilderException()
    {
        // Arrange
        var elements = Array.Empty<IFiniteElement>();
        const double delta = 0.0;

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => NodeMerger.MergeEqualNodes(elements, delta));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("There are no nodes to merge."));
    }

    [Test]
    public void MergeEqualNodes_ElementsWithoutNodes_ThrowsFeaModelBuilderException()
    {
        // Arrange
        var elements = new[] { CreateElement(), CreateElement() };
        const double delta = 0.0;

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => NodeMerger.MergeEqualNodes(elements, delta));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("There are no nodes to merge."));
    }

    [Test]
    public void MergeEqualNodes_WhenTwoEqualNodesInDifferentElements_SharesNodeFromFirstElement()
    {
        // Arrange
        var n1 = CreateNode(1, 2, 3);
        var n2 = CreateNode(1, 2, 3);

        var e2 = CreateElement(n2);
        var e1 = CreateElement(n1);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(e2.Nodes.ElementAt(0)),
            "После слияния оба элемента должны ссылаться на один экземпляр узла.");
    }

    [Test]
    public void MergeEqualNodes_WhenTwoEqualNodesInDifferentElementsWithinDelta_SharesNodeFromFirstElement()
    {
        // Arrange
        var n1 = CreateNode(0.0, 0.0, 0.0);
        var n2 = CreateNode(0.0005, 0.0005, 0.0005);
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(e2.Nodes.ElementAt(0)));
    }

    [Test]
    public void MergeEqualNodes_WhenTwoEqualNodesAndOneWithNodeId_SharesNodeWithId()
    {
        // Arrange
        var n1 = CreateNode(0.0, 0.0, 0.0);
        var n2 = CreateNode(0.0005, 0.0005, 0.0005, "Shared node");
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(n2));
        Assert.That(e1.Nodes.ElementAt(0).NodeId, Is.EqualTo("Shared node"));
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(e2.Nodes.ElementAt(0)));
    }

    [Test]
    public void MergeEqualNodes_WhenAllNodesOutsideDelta_AreNotMerged()
    {
        // Arrange
        var n1 = CreateNode(0.0, 0.0, 0.0);
        var n2 = CreateNode(0.01, 0.0, 0.0);
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e2.Nodes.ElementAt(0), Is.SameAs(n2));
        Assert.That(e1.Nodes.ElementAt(0), Is.Not.SameAs(e2.Nodes.ElementAt(0)));
    }

    [Test]
    public void MergeEqualNodes_WhenNodesNotEqualOnEachCoordinate_NodesNotMerged()
    {
        // Arrange
        var n1 = CreateNode(0.0, 0.0, 0.0);
        var n2 = CreateNode(0.0005, 0.0005, 5.0);
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e2.Nodes.ElementAt(0), Is.SameAs(n2));
    }

    [Test]
    public void MergeEqualNodes_MultipleGroups_MergedIndependently()
    {
        // Arrange
        var a1 = CreateNode(0, 0, 0);
        var a2 = CreateNode(0, 0, 0);
        var b1 = CreateNode(10, 0, 0);
        var b2 = CreateNode(10, 0, 0);

        var e1 = CreateElement(a1, b1);
        var e2 = CreateElement(a2, b2);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(e2.Nodes.ElementAt(0)));
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(a1));
        Assert.That(e1.Nodes.ElementAt(1), Is.SameAs(e2.Nodes.ElementAt(1)));
        Assert.That(e1.Nodes.ElementAt(1), Is.SameAs(b1));
        Assert.That(e1.Nodes.ElementAt(0), Is.Not.SameAs(e1.Nodes.ElementAt(1)));
    }

    // todo решить с Женей о поведении (остается два узла или один)
    [Test]
    public void MergeEqualNodes_TransitiveChain_AllMergedToOneGroup()
    {
        // Arrange
        // A ~ B (~0.0009), B ~ C (~0.0009), но A и C различаются на 0.0018 > delta.
        // DSU-логика должна объединить все три.
        var a = CreateNode(0.0, 0, 0);
        var b = CreateNode(0.0009, 0, 0);
        var c = CreateNode(0.0018, 0, 0);

        var ea = CreateElement(a);
        var eb = CreateElement(b);
        var ec = CreateElement(c);
        const double delta = 0.001;

        // Act
        NodeMerger.MergeEqualNodes([ea, eb, ec], delta);

        // Assert
        Assert.That(ea.Nodes.ElementAt(0), Is.SameAs(eb.Nodes.ElementAt(0)));
        Assert.That(eb.Nodes.ElementAt(0), Is.SameAs(ec.Nodes.ElementAt(0)));
    }

    [Test]
    public void MergeEqualNodes_SharedNodeInstance_RemainsSharedAfterMerge()
    {
        // Arrange
        var sharedNode = CreateNode(0, 0, 0);
        var e1 = CreateElement(sharedNode, CreateNode(1, 0, 0));
        var e2 = CreateElement(sharedNode, CreateNode(2, 0, 0));
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(e2.Nodes.ElementAt(0)));
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(sharedNode));
    }

    [Test]
    public void MergeEqualNodes_OneNodeWithId_UsesThatNodeAsShared()
    {
        // Arrange
        var withId = CreateNode(0, 0, 0, id: "N1");
        var withoutId = CreateNode(0, 0, 0);
        var e1 = CreateElement(withId);
        var e2 = CreateElement(withoutId);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        Assert.That(e1.Nodes.ElementAt(0), Is.SameAs(withId));
        Assert.That(e2.Nodes.ElementAt(0), Is.SameAs(withId));
    }

    [Test]
    public void MergeEqualNodes_TwoNodesWithDifferentIds_ThrowsFeaModelBuilderException()
    {
        // Arrange
        var n1 = CreateNode(0, 0, 0, id: "A");
        var n2 = CreateNode(0, 0, 0, id: "B");
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.0;

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => NodeMerger.MergeEqualNodes([e1, e2], delta));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Found 2 equal nodes with non-empty NodeId ('A', 'B'). " +
            "Only one node in a group of equal nodes may have a non-empty NodeId."));
    }

    [Test]
    public void MergeEqualNodes_NoNodesWithId_DoNotCreatesNewSharedNode()
    {
        // Arrange
        var n1 = CreateNode(1, 2, 3);
        var n2 = CreateNode(1, 2, 3);
        var e1 = CreateElement(n1);
        var e2 = CreateElement(n2);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e1, e2], delta);

        // Assert
        var shared = e1.Nodes.ElementAt(0);
        Assert.That(shared, Is.SameAs(e2.Nodes.ElementAt(0)));
        Assert.That(shared, Is.SameAs(n1));
        Assert.That(shared, Is.Not.SameAs(n2));
        Assert.That(shared.NodeId, Is.Empty);
        Assert.That(shared.X, Is.EqualTo(1));
        Assert.That(shared.Y, Is.EqualTo(2));
        Assert.That(shared.Z, Is.EqualTo(3));
    }

    [Test]
    public void MergeEqualNodes_SingleNodeGroup_DoesNotCreateNewNode()
    {
        // Arrange
        var singleNode = CreateNode(5, 6, 7);
        var e = CreateElement(singleNode);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e], delta);

        // Assert
        Assert.That(e.Nodes.ElementAt(0), Is.SameAs(singleNode));
    }

    [Test]
    public void MergeEqualNodes_DoesNotChangeNodeCountOfElement()
    {
        // Arrange
        var n1 = CreateNode(0, 0, 0);
        var n2 = CreateNode(0, 0, 0);
        var n3 = CreateNode(1, 1, 1);
        var e = CreateElement(n1, n2, n3);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e], delta);

        // Assert
        Assert.That(e.Nodes.Count, Is.EqualTo(3));
    }

    [Test]
    public void MergeEqualNodes_WhenNodesInsideSameElement_DoNotMerged()
    {
        // Arrange
        var n1 = CreateNode(0, 0, 0);
        var n2 = CreateNode(0.5, 0.5, 0.5);
        var n3 = CreateNode(2, 2, 2);
        var e = CreateElement(n1, n2, n3);
        const double delta = 1.0;

        // Act
        NodeMerger.MergeEqualNodes([e], delta);

        // Assert
        Assert.That(e.Nodes.Count, Is.EqualTo(3));
        Assert.That(e.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e.Nodes.ElementAt(1), Is.SameAs(n2));
        Assert.That(e.Nodes.ElementAt(2), Is.SameAs(n3));
    }

    [Test]
    public void MergeEqualNodes_PreservesNonEqualNodesInElement()
    {
        // Arrange
        var n1 = CreateNode(0, 0, 0);
        var n2 = CreateNode(0, 0, 0);
        var n3 = CreateNode(5, 5, 5);
        var e = CreateElement(n1, n2, n3);
        const double delta = 0.0;

        // Act
        NodeMerger.MergeEqualNodes([e], delta);

        // Assert
        Assert.That(e.Nodes.ElementAt(0), Is.SameAs(n1));
        Assert.That(e.Nodes.ElementAt(1), Is.SameAs(n2));
        Assert.That(e.Nodes.ElementAt(2), Is.SameAs(n3));
    }

    private static IElementNode CreateNode(
        double x, double y, double z,
        string id = "",
        ElementNodeType? type = null)
    {
        return new ElementNode(type ?? ElementNodeType.Type3D)
        {
            NodeId = id,
            X = x,
            Y = y,
            Z = z
        };
    }

    private static IFiniteElement CreateElement(params IElementNode[] nodes)
    {
        var nodesList = new List<IElementNode>(nodes);

        var elementMock = new Mock<IFiniteElement>();
        elementMock.SetupGet(e => e.Nodes).Returns(nodesList);

        return elementMock.Object;
    }
}