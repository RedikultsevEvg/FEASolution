using System.Reflection;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ElementNodes;
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
    public void Build_ShouldReturnElementWithCorrectNodeTypeAndDefaultUserId()
    {
        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element, Is.InstanceOf<FiniteElement>());
        Assert.That(element.Type.NodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(element.UserId, Is.EqualTo(string.Empty));
        Assert.That(element.Nodes, Has.Count.EqualTo(1));
    }

    [Test]
    public void Build_WithCustomUserId_ShouldSetUserId()
    {
        // Arrange
        const string userId = "user-123";
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build(userId);

        // Assert
        Assert.That(element.UserId, Is.EqualTo(userId));
    }

    [Test]
    public void Build_WhenCalledTwice_ShouldReturnDifferentInstances()
    {
        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        // Act
        var first = _builder.Build();
        var second = _builder.Build();

        // Assert
        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    public void Build_WithEmptyNodes_ShouldThrowNodeCollectionException()
    {
        // Arrange
        _builder.AddFreedom(Freedom.Temperature, CreateOptions());

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Can't build element with empty node collection. Use 'AddNode' to add nodes."));
    }

    [Test]
    public void Build_WithEmptyFreedoms_ShouldThrowFreedomCollectionException()
    {
        // Arrange
        _builder.AddNode(1.3, 2.4);

        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Can't build element with empty freedom collection. Use 'AddFreedom' to add freedoms."));
    }

    [Test]
    public void Build_WithEmptyNodesAndFreedoms_ShouldThrowNodeCollectionExceptionFirst()
    {
        // Act
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Can't build element with empty node collection. Use 'AddNode' to add nodes."));
    }

    [Test]
    public void Build_ShouldPreserveNodeCoordinates()
    {
        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.1, 2.2)
            .AddNode(3.3, 4.4);

        // Act
        var element = _builder.Build();

        // Assert
        var nodes = element.Nodes.ToList();
        Assert.That(nodes, Has.Count.EqualTo(2));
        Assert.That(nodes[0].X, Is.EqualTo(1.1));
        Assert.That(nodes[0].Y, Is.EqualTo(2.2));
        Assert.That(nodes[1].X, Is.EqualTo(3.3));
        Assert.That(nodes[1].Y, Is.EqualTo(4.4));
    }

    [Test]
    public void Build_WhenHasFreedoms_ShouldReturnElementWithCorrectFreedoms()
    {
        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddFreedom(Freedom.AnotherFreedom, CreateOptions())
            .AddNode(1.3, 2.4);

        // Act
        var element = _builder.Build();

        // Assert
        Assert.That(element.Type.Freedoms, Has.Count.EqualTo(2));
        Assert.That(element.Type.Freedoms.ElementAt(0).Item1, Is.EqualTo(Freedom.Temperature));
        Assert.That(element.Type.Freedoms.ElementAt(1).Item1, Is.EqualTo(Freedom.AnotherFreedom));
    }

    [Test]
    public void Build_ShouldCopyFreedoms_NotShareReference()
    {
        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        var element = _builder.Build();

        // Act
        _builder.AddFreedom(Freedom.AnotherFreedom, CreateOptions());

        // Assert
        Assert.That(element.Type.Freedoms, Has.Count.EqualTo(1),
            "Freedoms должны копироваться при сборке — добавление после Build не должно влиять на элемент.");
    }

    [Test]
    public void Build_ShouldShareNodesReference_KnownBug()
    {
        // Документирует текущее (потенциально ошибочное) поведение:
        // билдер передаёт свою коллекцию Nodes по ссылке в собранный элемент.
        // После Build добавление узла в билдер меняет и element.Nodes.
        // Если поведение будет исправлено — этот тест должен упасть и его нужно удалить.

        // Arrange
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        var element = _builder.Build();

        // Act
        _builder.AddNode(5.5, 6.6);

        // Assert
        Assert.That(element.Nodes, Has.Count.EqualTo(2),
            "Известный баг: Nodes разделяются по ссылке между билдером и элементом.");
    }

    #endregion

    #region AddFreedom

    [Test]
    public void AddFreedom_ShouldReturnSameBuilderInstance()
    {
        // Act
        var result = _builder.AddFreedom(Freedom.Temperature, CreateOptions());

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddFreedom_MultipleCalls_ShouldAccumulate()
    {
        // Act
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddFreedom(Freedom.AnotherFreedom, CreateOptions())
            .AddFreedom(Freedom.AnotherFreedom, CreateOptions());

        // Assert — Build не упадёт из-за freedooms, проверяем через собранный элемент
        _builder.AddNode(1.3, 2.4);
        var element = _builder.Build();
        Assert.That(element.Type.Freedoms, Has.Count.EqualTo(3));
    }

    [Test]
    public void AddFreedom_WithNullOptions_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<FeaElementBuilderException>(() =>
            _builder.AddFreedom(Freedom.Temperature, null!));
    }

    [Test]
    public void AddFreedom_WithDuplicateFreedom_ShouldAddTwice_KnownBehavior()
    {
        // Документирует текущее поведение: дубликаты freedom не отсеиваются.

        // Act
        _builder
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddFreedom(Freedom.Temperature, CreateOptions())
            .AddNode(1.3, 2.4);

        var element = _builder.Build();

        // Assert
        Assert.That(element.Type.Freedoms, Has.Count.EqualTo(2));
    }

    #endregion

    #region AddNode — 1D / 2D / 3D

    [Test]
    public void AddNode1D_ShouldStoreX_AndZeroYZ()
    {
        // Arrange
        const double expectedX = 3.14;

        // Act
        var result = _builder.AddNode(expectedX);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(result, Is.SameAs(_builder));
        Assert.That(node.X, Is.EqualTo(expectedX));
        Assert.That(node.Y, Is.EqualTo(0.0));
        Assert.That(node.Z, Is.EqualTo(0.0));
    }

    [Test]
    public void AddNode2D_ShouldStoreXY_AndZeroZ()
    {
        // Arrange
        const double expectedX = 1.5;
        const double expectedY = -3.25;

        // Act
        var result = _builder.AddNode(expectedX, expectedY);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(result, Is.SameAs(_builder));
        Assert.That(node.X, Is.EqualTo(expectedX));
        Assert.That(node.Y, Is.EqualTo(expectedY));
        Assert.That(node.Z, Is.EqualTo(0.0));
    }

    [Test]
    public void AddNode3D_ShouldStoreXYZ()
    {
        // Arrange
        const double expectedX = 1.1;
        const double expectedY = 2.2;
        const double expectedZ = 3.3;

        // Act
        var result = _builder.AddNode(expectedX, expectedY, expectedZ);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(result, Is.SameAs(_builder));
        Assert.That(node.X, Is.EqualTo(expectedX));
        Assert.That(node.Y, Is.EqualTo(expectedY));
        Assert.That(node.Z, Is.EqualTo(expectedZ));
    }

    [TestCase(-42.0)]
    [TestCase(0.0)]
    [TestCase(123.456)]
    public void AddNode1D_ShouldStoreAnyCoordinate(double x)
    {
        // Act
        _builder.AddNode(x);

        // Assert
        Assert.That(GetNodes(_builder).Single().X, Is.EqualTo(x));
    }

    [TestCase(0.0, 0.0)]
    [TestCase(-1.0, -2.0)]
    [TestCase(1.0, -2.0)]
    [TestCase(-1.0, 2.0)]
    public void AddNode2D_ShouldStoreAnyCoordinates(double x, double y)
    {
        // Act
        _builder.AddNode(x, y);

        // Assert
        var node = GetNodes(_builder).Single();
        Assert.That(node.X, Is.EqualTo(x));
        Assert.That(node.Y, Is.EqualTo(y));
    }

    [Test]
    public void AddNode1D_WhenCalledTwice_ShouldAddTwoNodes()
    {
        // Act
        _builder.AddNode(1.0).AddNode(2.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes, Has.Count.EqualTo(2));
        Assert.That(nodes[0].X, Is.EqualTo(1.0));
        Assert.That(nodes[1].X, Is.EqualTo(2.0));
    }

    [Test]
    public void AddNode2D_WhenCalledMultipleTimes_ShouldAddAllNodes()
    {
        // Act
        _builder.AddNode(0.0, 0.0)
            .AddNode(1.0, 0.0)
            .AddNode(1.0, 1.0)
            .AddNode(0.0, 1.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes, Has.Count.EqualTo(4));
        Assert.That(nodes[2].X, Is.EqualTo(1.0));
        Assert.That(nodes[2].Y, Is.EqualTo(1.0));
        Assert.That(nodes[3].X, Is.EqualTo(0.0));
        Assert.That(nodes[3].Y, Is.EqualTo(1.0));
    }

    [Test]
    public void AddNode3D_WhenCalledMultipleTimes_ShouldAddAllNodes()
    {
        // Act
        _builder.AddNode(0.0, 0.0, 0.0)
            .AddNode(1.0, 0.0, 0.0)
            .AddNode(0.0, 1.0, 0.0)
            .AddNode(0.0, 0.0, 1.0);

        // Assert
        var nodes = GetNodes(_builder).ToList();
        Assert.That(nodes, Has.Count.EqualTo(4));
        Assert.That(nodes[1].X, Is.EqualTo(1.0));
        Assert.That(nodes[2].Y, Is.EqualTo(1.0));
        Assert.That(nodes[3].Z, Is.EqualTo(1.0));
    }

    #endregion

    #region AddNode — mismatch dimension

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_1DThen2D_ShouldThrow()
    {
        // Arrange — первый AddNode(1D) фиксирует тип 1D
        _builder.AddNode(1.0);

        // Act & Assert
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));
        Assert.That(ex!.Message, Does.Contain("OneDimensional"));
    }

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_1DThen3D_ShouldThrow()
    {
        _builder.AddNode(1.0);

        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
        Assert.That(ex!.Message, Does.Contain("OneDimensional"));
    }

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_2DThen1D_ShouldThrow()
    {
        _builder.AddNode(1.0, 2.0);

        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
        Assert.That(ex!.Message, Does.Contain("TwoDimensional"));
    }

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_2DThen3D_ShouldThrow()
    {
        _builder.AddNode(1.0, 2.0);

        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0, 3.0));
        Assert.That(ex!.Message, Does.Contain("TwoDimensional"));
    }

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_3DThen1D_ShouldThrow()
    {
        _builder.AddNode(1.0, 2.0, 3.0);

        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
        Assert.That(ex!.Message, Does.Contain("ThreeDimensional"));
    }

    [Test]
    public void AddNode_DifferentDimensionsInSameBuilder_3DThen2D_ShouldThrow()
    {
        _builder.AddNode(1.0, 2.0, 3.0);

        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0, 2.0));
        Assert.That(ex!.Message, Does.Contain("ThreeDimensional"));
    }

    #endregion

    #region AddNode(IElementNode)

    [Test]
    public void AddNodeByInstance_ShouldAddNode()
    {
        // Arrange
        var node = new ElementNode(ElementNodeType.Type2D) { X = 1.0, Y = 2.0, NodeId = "n1" };

        // Act
        var result = _builder.AddNode(node);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
        Assert.That(GetNodes(_builder).Single(), Is.SameAs(node));
    }

    [Test]
    public void AddNodeByInstance_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _builder.AddNode(null!));
    }

    [Test]
    public void AddNodeByInstance_ShouldSetNodeTypeOnFirstCall()
    {
        // Arrange
        var node = new ElementNode(ElementNodeType.Type3D) { X = 1.0, Y = 2.0, Z = 3.0 };

        // Act
        _builder.AddNode(node);

        // Assert — второй узел другой размерности должен бросить
        Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(1.0));
    }

    [Test]
    public void AddNodeByInstance_WithMismatchedDimension_ShouldThrow()
    {
        // Arrange
        _builder.AddNode(1.0, 2.0);
        var node3D = new ElementNode(ElementNodeType.Type3D) { X = 1.0, Y = 2.0, Z = 3.0 };

        // Act & Assert
        var ex = Assert.Throws<FeaElementBuilderException>(() => _builder.AddNode(node3D));
        Assert.That(ex!.Message, Does.Contain("TwoDimensional"));
    }

    #endregion

    #region Helpers

    private static TemperatureElementOptions CreateOptions() => new()
    {
        ThermalConductivity = 2,
        Thickness = 1
    };

    private static ICollection<IElementNode> GetNodes(FiniteElementBuilder builder)
    {
        var property = typeof(FiniteElementBuilder)
            .GetProperty("Nodes", BindingFlags.NonPublic | BindingFlags.Instance);

        return (ICollection<IElementNode>)property!.GetValue(builder)!;
    }

    #endregion
}