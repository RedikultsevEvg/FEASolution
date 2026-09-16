using System.Reflection;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(ConstructionModelBuilder))]
public class ConstructionModelBuilderTests
{
    private ConstructionModelBuilder _builder = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new ConstructionModelBuilder();
    }

    #region Build

    [Test]
    public void Build_WhenNoNodeType_ShouldThrowFeaModelBuilderException()
    {
        // Act & Assert
        Assert.Throws<FeaModelBuilderException>(
            () => _builder.Build());
    }

    [Test]
    public void Build_WhenNodeTypeIs1D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void Build_WhenNodeTypeIs2D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void Build_WhenNodeTypeIs3D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void Build_AfterAddingElements_ReturnsModelWithThoseElements()
    {
        // Arrange
        var element = CreateTestElement(Dimensional.OneDimensional);
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddElements(element);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(1));
        Assert.That(model.Elements, Does.Contain(element));
    }

    [Test]
    public void Build_AfterAddingMultipleElements_ReturnsModelWithAllElements()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional);
        var second = CreateTestElement(Dimensional.OneDimensional);
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddElements(first, second);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(2));
        Assert.That(model.Elements, Does.Contain(first));
        Assert.That(model.Elements, Does.Contain(second));
    }

    [Test]
    public void Build_WhenCalledTwice_ReturnsDifferentInstances()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);

        // Act
        var first = _builder.Build();
        var second = _builder.Build();

        // Assert
        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    public void Build_PreservesAllowedNodeTypeSetByBuilder()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type2D);

        // Act
        var model = _builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
    }

    #endregion

    #region SetNodesType

    [Test]
    public void SetNodesType_WithValidType_ShouldReturnSameBuilderInstance()
    {
        // Act
        var result = _builder.SetNodesType(ElementNodeType.Type1D);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void SetNodesType_WhenNoElements_ShouldSucceed()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _builder.SetNodesType(ElementNodeType.Type2D));
    }

    [Test]
    public void SetNodesType_WhenElementsArePresent_ShouldThrowFeaModelBuilderException()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        _builder.AddElements(CreateTestElement(Dimensional.OneDimensional));

        // Act & Assert
        Assert.Throws<FeaModelBuilderException>(
            () => _builder.SetNodesType(ElementNodeType.Type2D));
    }

    [Test]
    public void SetNodesType_WhenElementsArePresent_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        _builder.AddElements(CreateTestElement(Dimensional.OneDimensional));

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.SetNodesType(ElementNodeType.Type2D));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("You have to remove all elements before changing node type."));
    }

    [Test]
    public void SetNodesType_WhenCalledMultipleTimesWithoutElements_ShouldSucceed()
    {
        // Act
        var result = _builder
            .SetNodesType(ElementNodeType.Type1D)
            .SetNodesType(ElementNodeType.Type2D)
            .SetNodesType(ElementNodeType.Type3D);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void SetNodesType_ShouldOverridePreviousType_WhenNoElementsAdded()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);

        // Act
        _builder.SetNodesType(ElementNodeType.Type2D);
        var model = _builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
    }

    [Test]
    public void SetNodesType_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.SetNodesType(null!));
    }

    #endregion

    #region AddElements

    [Test]
    public void AddElements_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddElements(null!));
    }

    [Test]
    public void AddElements_WithMatchingDimension_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var element = CreateTestElement(Dimensional.OneDimensional);

        // Act
        var result = _builder.AddElements(element);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [Test]
    public void AddElements_WithMismatchedDimension_ShouldThrowFeaModelBuilderException()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var element = CreateTestElement(Dimensional.TwoDimensional);

        // Act & Assert
        Assert.Throws<FeaModelBuilderException>(() => _builder.AddElements(element));
    }

    [Test]
    public void AddElements_WithMismatchedDimension_ShouldThrowWithExpectedMessage()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var element = CreateTestElement(Dimensional.TwoDimensional);

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(element));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Dimensional of all elements should be equal to OneDimensional."));
    }

    [Test]
    public void AddElements_WhenOneElementHasMismatchedDimension_ShouldNotAddAnyElement()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var valid = CreateTestElement(Dimensional.OneDimensional);
        var invalid = CreateTestElement(Dimensional.TwoDimensional);

        // Act
        Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(valid, invalid));

        // Assert
        Assert.That(GetElements(_builder), Is.Empty);
    }

    [Test]
    public void AddElements_WithMultipleMatchingElements_ShouldAddAll()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var first = CreateTestElement(Dimensional.OneDimensional);
        var second = CreateTestElement(Dimensional.OneDimensional);
        var third = CreateTestElement(Dimensional.OneDimensional);

        // Act
        _builder.AddElements(first, second, third);

        // Assert
        Assert.That(GetElements(_builder), Has.Count.EqualTo(3));
    }

    [Test]
    public void AddElements_WithEmptyCollection_ShouldReturnSameBuilderAndAddNothing()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);

        // Act
        var result = _builder.AddElements();

        // Assert
        Assert.That(result, Is.SameAs(_builder));
        Assert.That(GetElements(_builder), Is.Empty);
    }

    [Test]
    public void AddElements_WhenCalledMultipleTimes_ShouldAccumulateElements()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type1D);
        var first = CreateTestElement(Dimensional.OneDimensional);
        var second = CreateTestElement(Dimensional.OneDimensional);

        // Act
        _builder
            .AddElements(first)
            .AddElements(second);

        // Assert
        var elements = GetElements(_builder);
        Assert.That(elements, Has.Count.EqualTo(2));
        Assert.That(elements, Does.Contain(first));
        Assert.That(elements, Does.Contain(second));
    }

    [Test]
    public void AddElements_With2DElements_When2DTypeSet_ShouldSucceed()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type2D);

        // Act & Assert
        Assert.DoesNotThrow(
            () => _builder.AddElements(CreateTestElement(Dimensional.TwoDimensional)));
    }

    [Test]
    public void AddElements_With3DElements_When3DTypeSet_ShouldSucceed()
    {
        // Arrange
        _builder.SetNodesType(ElementNodeType.Type3D);

        // Act & Assert
        Assert.DoesNotThrow(
            () => _builder.AddElements(CreateTestElement(Dimensional.ThreeDimensional)));
    }

    [Test]
    public void AddElements_WhenNoNodeTypeSetAndElementIs1D_ShouldThrowWithDimensionalMessage()
    {
        var element = CreateTestElement(Dimensional.OneDimensional);

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(element));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Use method 'SetNodesType' to set the model node type."));
    }

    #endregion

    #region Helpers

    private static IFiniteElement CreateTestElement(Dimensional dimension)
    {
        var builder = new FiniteElementBuilder()
            .SetNodesType(GetNodeType(dimension));

        switch (dimension)
        {
            case Dimensional.OneDimensional:
                builder.AddNode(0.0);
                break;
            case Dimensional.TwoDimensional:
                builder.AddNode(0.0, 0.0);
                break;
            case Dimensional.ThreeDimensional:
                builder.AddNode(0.0, 0.0, 0.0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null);
        }

        return builder.Build();
    }

    private static ElementNodeType GetNodeType(Dimensional dimension) => dimension switch
    {
        Dimensional.OneDimensional => ElementNodeType.Type1D,
        Dimensional.TwoDimensional => ElementNodeType.Type2D,
        Dimensional.ThreeDimensional => ElementNodeType.Type3D,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null)
    };

    private static ICollection<IFiniteElement> GetElements(ConstructionModelBuilder builder)
    {
        var property = typeof(ConstructionModelBuilder)
            .GetProperty("Elements", BindingFlags.NonPublic | BindingFlags.Instance);

        return (ICollection<IFiniteElement>)property!.GetValue(builder)!;
    }

    #endregion
}