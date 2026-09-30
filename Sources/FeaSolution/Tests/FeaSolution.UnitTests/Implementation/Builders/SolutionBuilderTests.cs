using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.FiniteElements;
using Moq;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(SolutionBuilder))]
public class SolutionBuilderTests
{
    private Mock<IConstructionModel> _constructionModelMock = null!;

    [SetUp]
    public void SetUp()
    {
        _constructionModelMock = new Mock<IConstructionModel>();

        _constructionModelMock
            .Setup(i => i.Elements)
            .Returns(Array.Empty<FiniteElement>());
    }

    #region Assembly

    [Test]
    public void Assembly_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = new SolutionBuilder(_constructionModelMock.Object);

        // Act
        var result = builder.AssemblyAsync().Result;

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "Assembly must return the same builder instance to support fluent chaining.");
    }

    [Test]
    public void Assembly_ReturnsNonNull()
    {
        // Arrange
        var builder = new SolutionBuilder(_constructionModelMock.Object);

        // Act
        var result = builder.AssemblyAsync().Result;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Assembly_CanBeChainedMultipleTimes()
    {
        // Arrange
        var builder = new SolutionBuilder(_constructionModelMock.Object);

        // Act
        var result = builder
            .AssemblyAsync().Result
            .AssemblyAsync().Result
            .AssemblyAsync().Result;

        // Assert
        Assert.That(result, Is.SameAs(builder));
    }

    [Test]
    public void Assembly_DoesNotThrow()
    {
        // Arrange
        var builder = new SolutionBuilder(_constructionModelMock.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(() => builder.AssemblyAsync());
    }

    [Test]
    public void Assembly_CanBeChainedWithOtherBuilderMethods()
    {
        // Arrange
        var builder = new SolutionBuilder(_constructionModelMock.Object);

        // Act
        var result = builder
            .AssemblyAsync().Result
            .ValidateForCountOfElement()
            .ValidateForCommonElements()
            .AssemblyAsync().Result;

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "Assembly must integrate with other fluent methods without breaking the chain.");
    }

    #endregion
}