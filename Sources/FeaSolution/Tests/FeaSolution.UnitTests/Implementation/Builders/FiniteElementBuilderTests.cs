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

    #region CreateElement

    [Test]
    public void CreateElement_ShouldReturnNonNullElement()
    {
        // Act
        var element = _builder.CreateElement();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<IFiniteElement>());
    }

    [Test]
    public void CreateElement_WithDefaultUserId_ShouldCreateElement()
    {
        // Act
        var element = _builder.CreateElement();

        // Assert
        Assert.That(element, Is.InstanceOf<FiniteElement>());
    }

    [Test]
    public void CreateElement_WithCustomUserId_ShouldCreateElement()
    {
        // Arrange
        const string userId = "user-123";

        // Act
        var element = _builder.CreateElement(userId);

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<FiniteElement>());
    }

    [Test]
    public void CreateElement_WhenCalledTwice_ShouldReturnDifferentInstances()
    {
        // Act
        var first = _builder.CreateElement();
        var second = _builder.CreateElement();

        // Assert
        Assert.That(first, Is.Not.SameAs(second));
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
        var ex = Assert.Throws<FeaException>(() => _builder.SetNodesType(nodeType));

        // Assert
        Assert.That(ex!.Message, Does.Contain("remove all nodes"));
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

    #region AddNode

    [Test]
    public void AddNode_WithOneDimensionalNodeType_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.OneDimensional));

        // Act
        var result = _builder.AddNode(5.0);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddNode_WithNonOneDimensionalNodeType_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(CreateNodeType(Dimensional.TwoDimensional));

        // Act
        var ex = Assert.Throws<FeaException>(() => _builder.AddNode(1.0));

        // Assert
        Assert.That(ex!.Message, Does.Contain("another method"));
    }

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
    public void AddNode_ShouldStoreCorrectXCoordinate()
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
    public void AddNode_WithDifferentCoordinates_ShouldStoreEachCoordinate()
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
    public void AddNode_WithoutSettingNodeType_ShouldThrowBecauseDefaultIsNotOneDimensional()
    {
        // Act & Assert
        Assert.Throws<FeaException>(() => _builder.AddNode(1.0));
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