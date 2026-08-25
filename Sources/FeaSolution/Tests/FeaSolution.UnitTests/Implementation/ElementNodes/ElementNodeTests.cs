using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ElementNodes;

namespace FeaSolution.UnitTests.Implementation.ElementNodes;

[TestFixture]
[TestOf(typeof(ElementNode))]
public class ElementNodeTests
{
    [Test]
    public void Constructor_SetsType()
    {
        // Arrange
        var type = new ElementNodeType { Dimension = Dimensional.TwoDimensional };

        // Act
        var node = new ElementNode(type);

        // Assert
        Assert.AreEqual(Dimensional.TwoDimensional, node.Type.Dimension);
    }

    #region X-coordinate

    [Test]
    [TestCase(Dimensional.OneDimensional)]
    [TestCase(Dimensional.TwoDimensional)]
    [TestCase(Dimensional.ThreeDimensional)]
    public void X_CanBeSetForAllDimensions(Dimensional dimension)
    {
        // Arrange
        var node = CreateNewElementNode(dimension);

        // Act
        node.X = 5.0;

        // Assert
        Assert.AreEqual(5.0, node.X);
    }

    #endregion

    #region Y-coordinate

    [Test]
    public void Y_ReturnsZero_WhenOneDimensional()
    {
        // Arrange
        var node = CreateNewElementNode(Dimensional.OneDimensional);

        // Act
        var result = node.Y;

        // Assert
        Assert.AreEqual(0, result);
    }

    [Test]
    public void Y_Set_ThrowsFeaException_WhenOneDimensional()
    {
        // Arrange
        var node = CreateNewElementNode(Dimensional.OneDimensional);

        // Act & Assert
        Assert.Throws<FeaException>(() => node.Y = 3.0);
    }

    [Test]
    [TestCase(Dimensional.TwoDimensional)]
    [TestCase(Dimensional.ThreeDimensional)]
    public void Y_CanBeSet_WhenNotOneDimensional(Dimensional dimension)
    {
        // Arrange
        var node = CreateNewElementNode(dimension);

        // Act
        node.Y = 3.0;

        // Assert
        Assert.AreEqual(3.0, node.Y);
    }

    #endregion

    #region Z-coordinate

    [Test]
    [TestCase(Dimensional.OneDimensional)]
    [TestCase(Dimensional.TwoDimensional)]
    public void Z_ReturnsZero_WhenOneDimensionalOrTwoDimensional(Dimensional dimension)
    {
        // Arrange
        var node = CreateNewElementNode(dimension);

        // Act
        var result = node.Z;

        // Assert
        Assert.AreEqual(0, result);
    }

    [Test]
    public void Z_Set_ThrowsFeaException_WhenOneDimensional()
    {
        // Arrange
        var node = CreateNewElementNode(Dimensional.OneDimensional);

        // Act & Assert
        Assert.Throws<FeaException>(() => node.Z = 7.0);
    }

    [Test]
    public void Z_Set_ThrowsFeaException_WhenTwoDimensional()
    {
        // Arrange
        var node = CreateNewElementNode(Dimensional.TwoDimensional);

        // Act & Assert
        Assert.Throws<FeaException>(() => node.Z = 7.0);
    }

    [Test]
    public void Z_CanBeSet_WhenThreeDimensional()
    {
        // Arrange
        var node = CreateNewElementNode(Dimensional.ThreeDimensional);

        // Act
        node.Z = 7.0;

        // Assert
        Assert.AreEqual(7.0, node.Z);
    }

    #endregion

    private static ElementNode CreateNewElementNode(Dimensional dimension) =>
        new(new ElementNodeType { Dimension = dimension });
}
