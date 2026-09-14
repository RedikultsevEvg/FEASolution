using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModels;
using NSubstitute;

namespace FeaSolution.UnitTests.Implementation.ConstructionModels;

[TestFixture]
[TestOf(typeof(ConstructionModel))]
public class ConstructionModelTests
{
    [Test]
    public void AddElement_WhenElementIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new ConstructionModelBuilder().CreateModel();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => model.AddElement(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("The finite element can't be null"));
    }

    [Test]
    public void AddElement_WhenElementIsNotNull_AddsElementToCollection()
    {
        // Arrange
        var model = new ConstructionModelBuilder().CreateModel();
        var element = Substitute.For<IFiniteElement>();

        // Act
        model.AddElement(element);

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(1));
        Assert.That(model.Elements, Contains.Item(element));
    }

    [Test]
    public void AddElement_WhenCalledMultipleTimes_AddsAllElementsInOrder()
    {
        // Arrange
        var model = new ConstructionModelBuilder().CreateModel();
        var first = Substitute.For<IFiniteElement>();
        var second = Substitute.For<IFiniteElement>();
        var third = Substitute.For<IFiniteElement>();

        // Act
        model.AddElement(first).AddElement(second).AddElement(third);

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(3));
        Assert.That(model.Elements, Is.EqualTo([first, second, third]));
    }

    [Test]
    public void AddElement_ReturnsSameInstanceForFluentChaining()
    {
        // Arrange
        var model = new ConstructionModelBuilder().CreateModel();
        var element = Substitute.For<IFiniteElement>();

        // Act
        var result = model.AddElement(element);

        // Assert
        Assert.That(result, Is.SameAs(model));
    }

    [Test]
    public void Elements_WhenModelIsCreated_IsEmpty()
    {
        // Arrange & Act
        var model = new ConstructionModelBuilder().CreateModel();

        // Assert
        Assert.That(model.Elements, Is.Not.Null);
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void AllowedNodeType_WhenSetViaBuilder_ReturnsExpectedDimension()
    {
        // Arrange & Act
        var model1D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type1d).CreateModel();
        var model2D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type2d).CreateModel();
        var model3D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type3d).CreateModel();

        // Assert
        Assert.That(model1D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model2D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model3D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
    }

    [Test]
    public void Constructor_WhenInitializedWithRequiredProperty_SetsAllowedNodeType()
    {
        // Arrange
        var nodeType = ElementNodeType.Type1d;

        // Act
        var model = new ConstructionModel { AllowedNodeType = nodeType };

        // Assert
        Assert.That(model.AllowedNodeType, Is.SameAs(nodeType));
    }
}