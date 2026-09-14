using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
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
        var model = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateNew2D_SetsAllowedNodeTypeAndInitializesEmptyElements()
    {
        // Act
        var model = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2d)
            .CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateNew3D_SetsAllowedNodeTypeAndInitializesEmptyElements()
    {
        // Act
        var model = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3d)
            .CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
        Assert.That(model.Elements, Is.Empty);
    }
}