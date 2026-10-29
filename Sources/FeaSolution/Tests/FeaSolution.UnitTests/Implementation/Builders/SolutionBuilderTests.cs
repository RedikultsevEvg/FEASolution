using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ElementNodes;
using FeaSolution.Implementation.FiniteElements;
using Moq;

namespace FeaSolution.UnitTests.Implementation.Builders;

[TestFixture]
[TestOf(typeof(SolutionBuilder))]
public class SolutionBuilderTests
{
    private const string NotEnoughElementsMessage =
        "The construction model contains not enough elements (less then two).";

    private const string ModelNotDefinedMessage =
        "The construction model is not defined (equal null).";

    private Mock<IConstructionModel> _constructionModelMock = null!;

    [SetUp]
    public void SetUp()
    {
        _constructionModelMock = new Mock<IConstructionModel>();

        _constructionModelMock
            .Setup(i => i.CommonFreedoms)
            .Returns([Freedom.Temperature]);

        _constructionModelMock
            .Setup(i => i.Elements)
            .Returns([CreateTestElement("element 1"), CreateTestElement("element 2")]);
    }

    #region Assembly 

    [Test]
    public void Assembly_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder.Assembly();

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "Assembly must return the same builder instance to support fluent chaining.");
    }

    [Test]
    public void Assembly_CanBeChainedMultipleTimes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder
            .Assembly()
            .Assembly()
            .Assembly();

        // Assert
        Assert.That(result, Is.SameAs(builder));
    }

    [Test]
    public void Assembly_DoesNotThrow()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act & Assert
        Assert.DoesNotThrow(() => builder.Assembly());
    }

    [Test]
    public void Assembly_CanBeChainedWithOtherBuilderMethods()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder
            .Assembly()
            .ValidateForCountOfElement()
            .ValidateForCommonElements()
            .Assembly();

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "Assembly must integrate with other fluent methods without breaking the chain.");
    }

    #endregion

    #region ValidateForCountOfElement

    [Test]
    [Description("Метод должен возвращать тот же экземпляр builder для поддержки fluent-цепочек.")]
    public void ValidateForCountOfElement_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder.ValidateForCountOfElement();

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "ValidateForCountOfElement must return the same builder instance.");
    }

    [TestCase(2, TestName = "Exactly two elements - boundary minimum is valid")]
    [TestCase(3, TestName = "Three elements - above minimum")]
    [TestCase(10, TestName = "Many elements - well above minimum")]
    [Description("Метод не должен бросать исключение при количестве элементов >= 2.")]
    public void ValidateForCountOfElement_DoesNotThrow_WhenElementCountIsAtLeastTwo(int elementCount)
    {
        // Arrange
        var elements = Enumerable.Range(1, elementCount)
            .Select(i => CreateTestElement($"element {i}"))
            .ToArray();
        var builder = CreateBuilderWithElements(elements);

        // Act & Assert
        Assert.DoesNotThrow(() => builder.ValidateForCountOfElement());
    }

    [TestCase(0, TestName = "Zero elements")]
    [TestCase(1, TestName = "One element - below minimum")]
    [Description("Метод должен бросать FeaSolutionBuilderException при количестве элементов < 2.")]
    public void ValidateForCountOfElement_Throws_WhenElementCountIsLessThanTwo(int elementCount)
    {
        // Arrange
        var elements = Enumerable.Range(1, elementCount)
            .Select(i => CreateTestElement($"element {i}"))
            .ToArray();
        var builder = CreateBuilderWithElements(elements);

        // Act
        var ex = Assert.Throws<FeaSolutionBuilderException>(
            () => builder.ValidateForCountOfElement());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(NotEnoughElementsMessage));
    }

    [Test]
    [Description("Метод должен бросать FeaSolutionBuilderException с осмысленным сообщением, если constructionModel == null.")]
    public void ValidateForCountOfElement_Throws_WhenConstructionModelIsNull()
    {
        // Arrange
        var builder = new SolutionBuilder(null!);

        // Act
        var ex = Assert.Throws<FeaSolutionBuilderException>(
            () => builder.ValidateForCountOfElement());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(ModelNotDefinedMessage));
    }

    [Test]
    [Description("Метод не должен модифицировать состояние builder — повторный вызов даёт тот же результат.")]
    public void ValidateForCountOfElement_IsIdempotent()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var first = builder.ValidateForCountOfElement();
        var second = builder.ValidateForCountOfElement();

        // Assert
        Assert.That(first, Is.SameAs(builder));
        Assert.That(second, Is.SameAs(builder));
    }

    [Test]
    [Description("Метод должен корректно работать в цепочке с другими Validate-методами без зависимости от Assembly.")]
    public void ValidateForCountOfElement_CanBeChainedWithOtherValidationMethods()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var result = builder
            .ValidateForCountOfElement()
            .ValidateForCommonElements()
            .ValidateForZeroSquareElements()
            .ValidateForQualityOfElements()
            .ValidateForCountOfElement();

        // Assert
        Assert.That(result, Is.SameAs(builder),
            "ValidateForCountOfElement must integrate with other validation methods without breaking the chain.");
    }

    [Test]
    public void ValidateForCountOfElement_Throws_WhenElementsIsNull()
    {
        // Arrange
        _constructionModelMock.Setup(i => i.Elements).Returns((ICollection<IFiniteElement>)null!);
        var builder = CreateBuilder();

        // Act
        var ex = Assert.Throws<FeaSolutionBuilderException>(
            () => builder.ValidateForCountOfElement());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("The construction model element collection is not defined (equal null)."));
    }

    #endregion

    #region Test data factories

    private static IFiniteElement CreateTestElement(string elementId)
    {
        return new FiniteElement
        {
            Type = new FiniteElementType
            {
                NodeType = ElementNodeType.Type2D,
                Freedoms = [(Freedom.Temperature, new TemperatureElementOptions
                {
                    Thickness = 1,
                    ThermalConductivity = 1
                })]
            },
            Nodes =
            [
                CreateTestNode($"{elementId}-node-1", 0, 2),
                CreateTestNode($"{elementId}-node-2", 2, 2),
                CreateTestNode($"{elementId}-node-3", 2, 0),
            ],
            ElementId = elementId
        };
    }

    private static IElementNode CreateTestNode(string nodeId, double x, double y)
    {
        return new ElementNode(ElementNodeType.Type2D)
        {
            NodeId = nodeId,
            X = x,
            Y = y,
        };
    }

    /// <summary>
    /// Creates a builder with a model configured to return the supplied elements.
    /// </summary>
    private SolutionBuilder CreateBuilderWithElements(params IFiniteElement[] elements)
    {
        _constructionModelMock
            .Setup(i => i.Elements)
            .Returns(elements);

        return new SolutionBuilder(_constructionModelMock.Object);
    }

    private SolutionBuilder CreateBuilder() => new(_constructionModelMock.Object);

    #endregion
}