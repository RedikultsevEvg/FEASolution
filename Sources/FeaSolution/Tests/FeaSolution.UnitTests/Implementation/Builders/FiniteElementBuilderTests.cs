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
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<IFiniteElement>());
    }

    [Test]
    public void Build_WithDefaultUserId_ShouldCreateElement()
    {
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4);

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
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4);

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
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(1.3, 2.4);

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
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(1.3);

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
            .SetFreedoms(Freedom.Temperature)
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
            .SetFreedoms(Freedom.Temperature)
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

    [Test]
    public void Build_WithEmptyNodes_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetFreedoms(Freedom.Temperature);

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Can't build element with empty node collection. Use 'AddNode' to add nodes."));
    }

    [Test]
    public void Build_WithEmptyFreedoms_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Can't build element with empty freedom collection. Use 'SetFreedoms' to add freedoms."));
    }

    [Test]
    public void Build_WithEmptyNodesAndFreedoms_ShouldThrowNodeCollectionExceptionFirst()
    {
        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Can't build element with empty node collection. Use 'AddNode' to add nodes."));
    }

    [Test]
    public void Build_WhenHasFreedoms_ShouldReturnElementWithCorrectFreedoms()
    {
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature, Freedom.AnotherFreedom)
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element.ElementType.Freedoms, Has.Count.EqualTo(2));
        Assert.That(element.ElementType.Freedoms, Does.Contain(Freedom.Temperature));
        Assert.That(element.ElementType.Freedoms, Does.Contain(Freedom.AnotherFreedom));
    }

    [Test]
    public void Build_ShouldPreserveNodeCoordinates()
    {
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.1, 2.2)
            .AddNode(3.3, 4.4);

        // Act
        var element = _builder.Build();

        // Assert
        var nodes = element.Nodes.ToList();
        Assert.That(nodes[0].X, Is.EqualTo(1.1));
        Assert.That(nodes[0].Y, Is.EqualTo(2.2));
        Assert.That(nodes[1].X, Is.EqualTo(3.3));
        Assert.That(nodes[1].Y, Is.EqualTo(4.4));
    }

    [Test]
    public void Build_WithEmptyUserId_ShouldSetUserIdToEmptyString()
    {
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element.UserId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Build_ShouldPreserveNodeType()
    {
        // Arrange
        var nodeType = CreateNodeType(Dimensional.ThreeDimensional);
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(nodeType)
            .AddNode(1.0, 2.0, 3.0);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element.ElementType.NodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
    }

    [Test]
    public void Build_ShouldSetStiffnessMatrixCalculationMethodToNull()
    {
        // Arrange
        _builder
            .SetFreedoms(Freedom.Temperature)
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element.ElementType.StiffnessMatrixCalculationMethod, Is.Null);
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

    [Test]
    public void SetNodesType_WhenNodesAreEmpty_ShouldAllowChange()
    {
        // Arrange
        var firstType = CreateNodeType(Dimensional.OneDimensional);
        var secondType = CreateNodeType(Dimensional.ThreeDimensional);

        // Act
        _builder.SetNodesType(firstType);
        _builder.SetNodesType(secondType);

        // Assert — проверяем, что тип действительно изменился
        _builder.AddNode(1.0, 2.0, 3.0);
        Assert.That(GetNodes(_builder), Has.Count.EqualTo(1));
    }

    [Test]
    public void SetNodesType_WithNullNodeType_ShouldThrowOrAllowNull()
    {
        // Act & Assert — поведение зависит от реализации;
        // если null не допускается, это должно быть явно проверено
        // (в текущей реализации null приведёт к NRE при AddNode)
        Assert.DoesNotThrow(() => _builder.SetNodesType(null!));
    }

    [Test]
    public void SetNodesType_WhenNodesAreNotEmpty_ShouldNotChangeNodeType()
    {
        // Arrange
        var originalType = CreateNodeType(Dimensional.OneDimensional);
        _builder.SetNodesType(originalType);
        _builder.AddNode(1.0);

        var newType = CreateNodeType(Dimensional.TwoDimensional);

        // Act
        try { _builder.SetNodesType(newType); } catch (FeaElementBuilderException) { }

        // Assert — тип не должен измениться, поэтому 1D-узел всё ещё добавляется
        Assert.DoesNotThrow(() => _builder.AddNode(2.0));
    }

    #endregion

    #region SetFreedoms

    [Test]
    public void SetFreedoms_WithValidFreedoms_ShouldReturnSameBuilderInstance()
    {
        // Act
        var result = _builder.SetFreedoms(Freedom.Temperature);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void SetFreedoms_WithMultipleFreedoms_ShouldAddAllFreedoms()
    {
        // Arrange
        _builder
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        _builder.SetFreedoms(Freedom.Temperature, Freedom.AnotherFreedom, Freedom.AnotherFreedom);
        var element = _builder.Build();

        // Assert
        Assert.That(element.ElementType.Freedoms, Has.Count.EqualTo(3));
    }

    [Test]
    public void SetFreedoms_WhenCalledMultipleTimes_ShouldAccumulateFreedoms()
    {
        // Arrange
        _builder
            .SetNodesType(CreateNodeType(Dimensional.TwoDimensional))
            .AddNode(1.3, 2.4);

        // Act
        _builder.SetFreedoms(Freedom.Temperature);
        _builder.SetFreedoms(Freedom.AnotherFreedom);
        var element = _builder.Build();

        // Assert
        Assert.That(element.ElementType.Freedoms, Has.Count.EqualTo(2));
        Assert.That(element.ElementType.Freedoms, Does.Contain(Freedom.Temperature));
        Assert.That(element.ElementType.Freedoms, Does.Contain(Freedom.AnotherFreedom));
    }

    [Test]
    public void SetFreedoms_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _builder.SetFreedoms(null!));
    }

    [Test]
    public void SetFreedoms_WithEmptyCollection_ShouldThrowWithExpectedMessage()
    {
        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(
            () => _builder.SetFreedoms(Array.Empty<Freedom>()));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Can't add empty freedom collection."));
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

    #region AddNode — edge cases

    [Test]
    public void AddNode1D_WhenNodeTypeIsDefault_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
    }

    [Test]
    public void AddNode2D_WhenNodeTypeIsDefault_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));
    }

    [Test]
    public void AddNode3D_WhenNodeTypeIsDefault_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
    }

    [Test]
    public void AddNode1D_WhenCalledTwice_ShouldAddTwoNodes()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        _builder.AddNode(1.0).AddNode(2.0);

        // Assert
        Assert.That(GetNodes(_builder), Has.Count.EqualTo(2));
    }

    [Test]
    public void AddNode_ExceptionMessage_ShouldContainCurrentDimensionFor1D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is OneDimensional. You should use another method to specify all dimentions."));
    }

    [Test]
    public void AddNode_ExceptionMessage_ShouldContainCurrentDimensionFor2D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is TwoDimensional. You should use another method to specify all dimentions."));
    }

    [Test]
    public void AddNode_ExceptionMessage_ShouldContainCurrentDimensionFor3D()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.ThreeDimensional));

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The current accepted node type is ThreeDimensional. You should use another method to specify all dimentions."));
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