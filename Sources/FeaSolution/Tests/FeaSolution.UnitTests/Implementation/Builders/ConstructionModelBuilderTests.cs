using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(ConstructionModelBuilder))]
public class ConstructionModelBuilderTests
{
    [Test]
    public void CreateModel_WhenNodeTypeIs1D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D);

        // Act
        var model = builder.CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.OneDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateModel_WhenNodeTypeIs2D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D);

        // Act
        var model = builder.CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
        Assert.That(model.Elements, Is.Empty);
    }

    [Test]
    public void CreateModel_WhenNodeTypeIs3D_ReturnsTheModel()
    {
        // Arrange
        var builder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D);

        // Act
        var model = builder.CreateModel();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.ThreeDimensional));
        Assert.That(model.Elements, Is.Empty);
    }
}