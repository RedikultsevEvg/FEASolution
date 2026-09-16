using System.Reflection;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.FiniteElements;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(FiniteElementBuilder))]
public class FiniteElementBuilderTests
{
    private FiniteElementBuilder _builder = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new FiniteElementBuilder();
    }

    #region Build

    [Test]
    public void Build_ShouldReturnNonNullElement()
    {
        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<IFiniteElement>());
    }

    [Test]
    public void Build_WithDefaultUserId_ShouldCreateElement()
    {
        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.InstanceOf<FiniteElement>());
    }

    [Test]
    public void Build_WithCustomUserId_ShouldCreateElement()
    {
        // Arrange
        const string userId = "user-123";

        // Act
        var element = _builder.Build(userId);

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<FiniteElement>());
        Assert.That(element.UserId, Is.EqualTo(userId));
    }

    [Test]
    public void Build_WhenCalledTwice_ShouldReturnDifferentInstances()
    {
        // Act
        var first = _builder.Build();
        var second = _builder.Build();

        // Assert
        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    public void Build_WhenHasNodeType_ShouldReturnElementWithCorrectNodeType()
    {
        // Arrange
        _builder
            .SetNodesType(ElementNodeType.Type1D);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element.ElementType.NodeType, Is.EqualTo(ElementNodeType.Type1D));
    }

    [Test]
    public void Build_WhenHasOneNode_ShouldReturnElementWithOneNode()
    {
        // Arrange
        _builder
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element.Nodes, Has.Count.EqualTo(1));
    }

    [Test]
    public void Build_WhenHasTreeNodes_ShouldReturnElementWithTreeNode()
    {
        // Arrange
        _builder
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4)
            .AddNode(2.3, 3.4)
            .AddNode(3.3, 4.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element.Nodes, Has.Count.EqualTo(3));
    }

    #endregion

    #region SetNodesType

    [Test]
    public void SetNodesType_WithValidType_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var nodeType = CreateNodeType(Dimensional.OneDimensional);

        // Act
        var result = _builder.SetNodesType(nodeType);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void SetNodesType_WhenNodesAreNotEmpty_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        var nodeType = CreateNodeType(Dimensional.OneDimensional);
        _builder.SetNodesType(nodeType);
        _builder.AddNode(1.0);

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.SetNodesType(nodeType));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("You have to remove all nodes before changing node type."));
    }

    [Test]
    public void SetNodesType_WhenCalledMultipleTimesWithoutNodes_ShouldSucceed()
    {
        // Arrange
        var firstType = CreateNodeType(Dimensional.OneDimensional);
        var secondType = CreateNodeType(Dimensional.TwoDimensional);

        // Act
        var result = _builder
            .SetNodesType(firstType)
            .SetNodesType(secondType);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    #endregion

    #region AddNode — common tests

    [Test]
    public void AddNode_WhenCalledMultipleTimes_ShouldAddNodesToCollection()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(0.0);
        _builder.AddNode(1.0);
        _builder.AddNode(2.0);

        // Assert
        var nodes = GetNodes(_builder);
        Assert.That(nodes, Has.Count.EqualTo(3));
    }

    [Test]
    public void AddNode_WithoutSettingNodeType_ShouldThrowBecauseDefaultIsNotOneDimensional()
    {
        // Act & Assert
        var e = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
        Assert.That(e.Message, Is.EqualTo("The current accepted node type is 0. You should use another method to specify all dimentions."));
    }

    #endregion

    #region AddNode — 1D

    [Test]
    public void AddNode1D_WithOneDimensionalNodeType_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        var result = _builder.AddNode(5.0);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddNode1D_WithNonOneDimensionalNodeType_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is TwoDimensional. You should use another method to specify all dimentions."));
    }

    [Test]
    public void AddNode1D_ShouldStoreCorrectXCoordinate()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));
        const double expectedX = 3.14;

        // Act
        _builder.AddNode(expectedX);

        // Assert
        var nodes = GetNodes(_builder);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes.First().X, Is.EqualTo(expectedX));
    }

    [Test]
    public void AddNode1D_WithDifferentCoordinates_ShouldStoreEachCoordinate()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(1.5);
        _builder.AddNode(-2.5);
        _builder.AddNode(0.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes[0].X, Is.EqualTo(1.5));
        Assert.That(nodes[1].X, Is.EqualTo(-2.5));
        Assert.That(nodes[2].X, Is.EqualTo(0.0));
    }

    [Test]
    public void AddNode1D_ShouldNotSetYAndZCoordinates()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(7.0);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.Y, Is.EqualTo(0.0));
        Assert.That(node.Z, Is.EqualTo(0.0));
    }

    [Test]
    public void AddNode1D_WithNegativeCoordinate_ShouldStoreNegativeValue()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(-42.0);

        // Assert
        Assert.That(GetNodes(_builder).Single().X, Is.EqualTo(-42.0));
    }

    [Test]
    public void AddNode1D_WithZeroCoordinate_ShouldStoreZero()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(0.0);

        // Assert
        Assert.That(GetNodes(_builder).Single().X, Is.EqualTo(0.0));
    }

    #endregion

    #region AddNode — 2D

    [Test]
    public void AddNode2D_WithTwoDimensionalNodeType_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        var result = _builder.AddNode(1.0, 2.0);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddNode2D_WithNonTwoDimensionalNodeType_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is OneDimensional. You should use another method to specify all dimentions."));
    }

    [Test]
    public void AddNode2D_WithThreeDimensionalNodeType_ShouldThrow()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));
    }

    [Test]
    public void AddNode2D_ShouldStoreCorrectCoordinates()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));
        const double expectedX = 1.5;
        const double expectedY = -3.25;

        // Act
        _builder.AddNode(expectedX, expectedY);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(expectedX));
        Assert.That(node.Y, Is.EqualTo(expectedY));
    }

    [Test]
    public void AddNode2D_ShouldNotSetZCoordinate()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        _builder.AddNode(1.0, 2.0);

        // Assert
        Assert.That(GetNodes(_builder).Single().Z, Is.EqualTo(0.0));
    }

    [Test]
    public void AddNode2D_WhenCalledMultipleTimes_ShouldAddAllNodes()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        _builder.AddNode(0.0, 0.0);
        _builder.AddNode(1.0, 0.0);
        _builder.AddNode(1.0, 1.0);
        _builder.AddNode(0.0, 1.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes, Has.Count.EqualTo(4));
        Assert.That(nodes[0].X, Is.EqualTo(0.0));
        Assert.That(nodes[0].Y, Is.EqualTo(0.0));
        Assert.That(nodes[2].X, Is.EqualTo(1.0));
        Assert.That(nodes[2].Y, Is.EqualTo(1.0));
        Assert.That(nodes[3].X, Is.EqualTo(0.0));
        Assert.That(nodes[3].Y, Is.EqualTo(1.0));
    }

    [Test]
    public void AddNode2D_WithNegativeCoordinates_ShouldStoreNegativeValues()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        _builder.AddNode(-1.0, -2.0);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(-1.0));
        Assert.That(node.Y, Is.EqualTo(-2.0));
    }

    [Test]
    public void AddNode2D_WithZeroCoordinates_ShouldStoreZeros()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        _builder.AddNode(0.0, 0.0);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(0.0));
        Assert.That(node.Y, Is.EqualTo(0.0));
    }

    #endregion

    #region AddNode — 3D

    [Test]
    public void AddNode3D_WithThreeDimensionalNodeType_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));

        // Act
        var result = _builder.AddNode(1.0, 2.0, 3.0);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddNode3D_WithNonThreeDimensionalNodeType_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is OneDimensional. You should use another method to specify all dimentions."));
    }

    [Test]
    public void AddNode3D_WithTwoDimensionalNodeType_ShouldThrow()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
    }

    [Test]
    public void AddNode3D_ShouldStoreCorrectXAndYCoordinates()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));
        const double expectedX = 1.1;
        const double expectedY = 2.2;

        // Act
        _builder.AddNode(expectedX, expectedY, 3.3);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(expectedX));
        Assert.That(node.Y, Is.EqualTo(expectedY));
    }

    [Test]
    public void AddNode3D_ShouldStoreCorrectZCoordinate()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));
        const double expectedZ = 3.3;

        // Act
        _builder.AddNode(1.1, 2.2, expectedZ);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.Z, Is.EqualTo(expectedZ));
    }

    [Test]
    public void AddNode3D_WhenCalledMultipleTimes_ShouldAddAllNodes()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));

        // Act
        _builder.AddNode(0.0, 0.0, 0.0);
        _builder.AddNode(1.0, 0.0, 0.0);
        _builder.AddNode(0.0, 1.0, 0.0);
        _builder.AddNode(0.0, 0.0, 1.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes, Has.Count.EqualTo(4));
        Assert.That(nodes[1].X, Is.EqualTo(1.0));
        Assert.That(nodes[2].Y, Is.EqualTo(1.0));
        Assert.That(nodes[3].Z, Is.EqualTo(1.0));
    }

    [Test]
    public void AddNode3D_WithNegativeCoordinates_ShouldStoreNegativeValues()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));

        // Act
        _builder.AddNode(-1.0, -2.0, -3.0);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(-1.0));
        Assert.That(node.Y, Is.EqualTo(-2.0));
        Assert.That(node.Z, Is.EqualTo(-3.0));
    }

    #endregion

    #region AddNode — cross-check

    [Test]
    public void AddNode_1DMethod_ShouldNotBeUsableWhenTypeIs2D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
    }

    [Test]
    public void AddNode_2DMethod_ShouldNotBeUsableWhenTypeIs1D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));
    }

    [Test]
    public void AddNode_3DMethod_ShouldNotBeUsableWhenTypeIs1D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
    }

    [Test]
    public void AddNode_3DMethod_ShouldNotBeUsableWhenTypeIs2D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
    }

    [Test]
    public void AddNode_ExceptionMessage_ShouldContainCurrentDimension()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));

        // Assert
        Assert.That(ex!.Message, Does.Contain(Dimensional.TwoDimensional.ToString()));
    }

    #endregion

    #region Helpers

    private static ElementNodeType CreateNodeType(Dimensional dimension)
    {
        return new ElementNodeType
        {
            Dimension = dimension
        };
    }

    private static ICollection<IElementNode> GetNodes(FiniteElementBuilder builder)
    {
        var property = typeof(FiniteElementBuilder)
            .GetProperty("Nodes", BindingFlags.NonPublic | BindingFlags.Instance);

        return (ICollection<IElementNode>)property!.GetValue(builder)!;
    }

    #endregion
}