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