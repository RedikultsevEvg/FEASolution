using FeaSolution.Core.Enums;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(ConstructionModelBuilder))]
public class ConstructionModelBuilderTests
{
    [Test]
    public void CreateNew1D_SetsAllowedNodeTypeAndInitializesEmptyElements()
    {
        // Act
        var model = ConstructionModelBuilder.CreateNew1D();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateNew2D_SetsAllowedNodeTypeAndInitializesEmptyElements()
    {
        // Act
        var model = ConstructionModelBuilder.CreateNew2D();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateNew3D_SetsAllowedNodeTypeAndInitializesEmptyElements()
    {
        // Act
        var model = ConstructionModelBuilder.CreateNew3D();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
        Assert.That(model.Elements, Is.Empty);
    }
}