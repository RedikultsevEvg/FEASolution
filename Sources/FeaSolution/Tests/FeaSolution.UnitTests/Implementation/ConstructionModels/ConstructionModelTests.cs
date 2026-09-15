using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.UnitTests.Implementation.ConstructionModels;

[TestFixture]
[TestOf(typeof(ConstructionModel))]
public class ConstructionModelTests
{
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
        var model1D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type1D).CreateModel();
        var model2D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type2D).CreateModel();
        var model3D = new ConstructionModelBuilder().SetNodesType(ElementNodeType.Type3D).CreateModel();

        // Assert
        Assert.That(model1D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model2D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model3D.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
    }

    [Test]
    public void Constructor_WhenInitializedWithRequiredProperty_SetsAllowedNodeType()
    {
        // Arrange
        var nodeType = ElementNodeType.Type1D;

        // Act
        var model = new ConstructionModel { AllowedNodeType = nodeType };

        // Assert
        Assert.That(model.AllowedNodeType, Is.SameAs(nodeType));
    }
}