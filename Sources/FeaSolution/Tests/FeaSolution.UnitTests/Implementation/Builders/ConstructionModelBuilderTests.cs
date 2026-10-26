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
    private const string EmptyElementsMessage =
        "Can't build the model with empty element collection. Use 'AddElements' to add elements.";

    private const string NoCommonFreedomsMessage =
        "Can't build the model - all added elements should have at least one common freedom.";

    private const string EmptyCollectionOnAddMessage =
        "Can't add an empty element collection. Pass at least one element.";

    private ConstructionModelBuilder _builder = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new ConstructionModelBuilder();
    }

    #region Build

    [Test]
    [Description("Build должен бросать FeaModelBuilderException с ожидаемым сообщением, если элементы не добавлены.")]
    public void Build_WhenNoElementsAdded_ThrowsWithExpectedMessage()
    {
        // Arrange — эмулируем «NodeType установлен, но элементов нет» через рефлексию,
        // т.к. публичного API для этого нет.
        SetNodeTypeViaReflection(_builder, CreateNodeType(Dimensional.OneDimensional));

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(() => _builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(EmptyElementsMessage));
    }

    [TestCase(Dimensional.OneDimensional, TestName = "Build with 1D element returns model")]
    [TestCase(Dimensional.TwoDimensional, TestName = "Build with 2D element returns model")]
    [TestCase(Dimensional.ThreeDimensional, TestName = "Build with 3D element returns model")]
    [Description("Build должен возвращать модель с корректной размерностью для любого поддерживаемого Dimensional.")]
    public void Build_ReturnsModelWithCorrectDimension(Dimensional dimension)
    {
        // Arrange
        var builder = CreateBuilderWithElements(dimension, Freedom.Temperature);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(dimension));
        Assert.That(model.Elements, Has.Count.EqualTo(1));
    }

    [Test]
    [Description("Build должен сохранять все добавленные элементы.")]
    public void Build_AfterAddingMultipleElements_ReturnsModelWithAllElements()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional);
        var second = CreateTestElement(Dimensional.OneDimensional);
        var builder = new ConstructionModelBuilder().AddElements(first, second);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(2));
        Assert.That(model.Elements, Does.Contain(first));
        Assert.That(model.Elements, Does.Contain(second));
    }

    [Test]
    [Description("Каждый вызов Build создаёт новый экземпляр модели.")]
    public void Build_WhenCalledTwice_ReturnsDifferentInstances()
    {
        // Arrange
        _builder.AddElements(CreateTestElement(Dimensional.OneDimensional));

        // Act
        var first = _builder.Build();
        var second = _builder.Build();

        // Assert
        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    [Description("Build должен заполнить CommonFreedoms пересечением свобод элементов.")]
    public void Build_WhenElementsShareOneFreedom_PopulatesCommonFreedoms()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.AnotherFreedom);
        var second = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.NotSupported);
        var builder = new ConstructionModelBuilder().AddElements(first, second);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.CommonFreedoms, Is.Not.Null);
        Assert.That(model.CommonFreedoms, Has.Count.EqualTo(1));
        Assert.That(model.CommonFreedoms, Does.Contain(Freedom.Temperature));
    }

    [Test]
    [Description("Build должен вернуть все общие свободы, если их несколько.")]
    public void Build_WhenElementsShareMultipleFreedoms_ReturnsAllCommonFreedoms()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.AnotherFreedom);
        var second = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.AnotherFreedom);
        var builder = new ConstructionModelBuilder().AddElements(first, second);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.CommonFreedoms, Has.Count.EqualTo(2));
        Assert.That(model.CommonFreedoms,
            Is.EquivalentTo([Freedom.Temperature, Freedom.AnotherFreedom]));
    }

    [Test]
    [Description("Build должен бросить исключение с ожидаемым сообщением, если у элементов нет общих свобод.")]
    public void Build_WhenElementsHaveNoCommonFreedoms_ThrowsWithExpectedMessage()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional, Freedom.Temperature);
        var second = CreateTestElement(Dimensional.OneDimensional, Freedom.AnotherFreedom);
        var builder = new ConstructionModelBuilder().AddElements(first, second);

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(() => builder.Build());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(NoCommonFreedomsMessage));
    }

    [Test]
    [Description("Build должен учитывать пересечение свобод для 3+ элементов, а не только пар.")]
    public void Build_WhenThreeElementsOnlyTwoShareFreedom_Throws()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.AnotherFreedom);
        var second = CreateTestElement(Dimensional.OneDimensional, Freedom.Temperature);
        var third = CreateTestElement(Dimensional.OneDimensional, Freedom.AnotherFreedom);
        var builder = new ConstructionModelBuilder().AddElements(first, second, third);

        // Act & Assert
        Assert.Throws<FeaModelBuilderException>(() => builder.Build());
    }

    [Test]
    [Description("Для одного элемента все его свободы являются общими.")]
    public void Build_WhenSingleElement_ReturnsAllItsFreedoms()
    {
        // Arrange
        var element = CreateTestElement(Dimensional.OneDimensional,
            Freedom.Temperature, Freedom.AnotherFreedom);
        var builder = new ConstructionModelBuilder().AddElements(element);

        // Act
        var model = builder.Build();

        // Assert
        Assert.That(model.CommonFreedoms, Has.Count.EqualTo(2));
        Assert.That(model.CommonFreedoms,
            Is.EquivalentTo([Freedom.Temperature, Freedom.AnotherFreedom]));
    }

    [Test]
    [Description("Build после нескольких AddElements должен собрать модель со всеми элементами и корректной размерностью.")]
    public void Build_AfterMultipleAddElementsCalls_ReturnsAllElements()
    {
        // Arrange
        _builder
            .AddElements(CreateTestElement(Dimensional.TwoDimensional))
            .AddElements(CreateTestElement(Dimensional.TwoDimensional))
            .AddElements(CreateTestElement(Dimensional.TwoDimensional));

        // Act
        var model = _builder.Build();

        // Assert
        Assert.That(model.Elements, Has.Count.EqualTo(3));
        Assert.That(model.AllowedNodeType.Dimension, Is.EqualTo(Dimensional.TwoDimensional));
    }

    #endregion

    #region AddElements — валидация аргументов

    [Test]
    [Description("AddElements должен бросать ArgumentNullException при null-коллекции.")]
    public void AddElements_WithNullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddElements(null!));
    }

    [Test]
    [Description("AddElements должен бросать FeaModelBuilderException на пустой коллекции.")]
    public void AddElements_WithEmptyCollection_ThrowsWithExpectedMessage()
    {
        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(() => _builder.AddElements());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(EmptyCollectionOnAddMessage));
    }

    [Test]
    [Description("AddElements должен бросать FeaModelBuilderException, если коллекция содержит null-элемент.")]
    public void AddElements_WithNullElementInCollection_ThrowsWithExpectedMessage()
    {
        // Arrange
        var valid = CreateTestElement(Dimensional.OneDimensional);

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(valid, null!));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Element collection contains null elements."));
    }

    #endregion

    #region AddElements — размерности

    [Test]
    [Description("AddElements не должен добавлять элементы при конфликте размерностей внутри одного вызова.")]
    public void AddElements_WhenElementsWithinCallHaveDifferentDimensions_DoesNotAddAny()
    {
        // Arrange
        var valid = CreateTestElement(Dimensional.OneDimensional);
        var invalid = CreateTestElement(Dimensional.TwoDimensional);

        // Act
        Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(valid, invalid));

        // Assert
        Assert.That(GetElements(_builder), Is.Empty);
    }

    [Test]
    [Description("AddElements должен сообщить обе конфликтующие размерности в тексте ошибки.")]
    public void AddElements_WhenElementsWithinCallHaveDifferentDimensions_ReportsBothDimensions()
    {
        // Arrange
        var e2D = CreateTestElement(Dimensional.TwoDimensional);
        var e3D = CreateTestElement(Dimensional.ThreeDimensional);

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(e2D, e3D));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("All added elements should have the same dimension. Found dimensions: TwoDimensional, ThreeDimensional."));
    }

    [TestCase(Dimensional.OneDimensional, Dimensional.TwoDimensional, Dimensional.ThreeDimensional,
        TestName = "AddElements with 1D+2D+3D throws")]
    [TestCase(Dimensional.TwoDimensional, Dimensional.ThreeDimensional, Dimensional.TwoDimensional,
        TestName = "AddElements with 2D+3D+2D throws")]
    [Description("AddElements должен бросать исключение при более чем двух различных размерностях.")]
    public void AddElements_WhenMultipleDifferentDimensions_Throws(
        Dimensional d1, Dimensional d2, Dimensional d3)
    {
        Assert.Throws<FeaModelBuilderException>(() => _builder.AddElements(
            CreateTestElement(d1),
            CreateTestElement(d2),
            CreateTestElement(d3)));
    }

    [Test]
    [Description("AddElements должен отклонять элементы иной размерности, чем уже настроенный NodeType.")]
    public void AddElements_WhenDimensionConflictsWithConfiguredNodeType_ThrowsWithExpectedMessage()
    {
        // Arrange
        _builder.AddElements(CreateTestElement(Dimensional.TwoDimensional));

        // Act
        var ex = Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(CreateTestElement(Dimensional.ThreeDimensional)));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo(
            "Dimension of added elements (ThreeDimensional) should be equal to the already configured node type dimension (TwoDimensional)."));
    }

    [Test]
    [Description("AddElements не должен добавлять элементы, если размерность конфликтует с уже настроенным NodeType.")]
    public void AddElements_WhenDimensionConflictsWithConfiguredNodeType_KeepsPreviousElements()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.TwoDimensional);
        _builder.AddElements(first);

        // Act
        Assert.Throws<FeaModelBuilderException>(
            () => _builder.AddElements(CreateTestElement(Dimensional.ThreeDimensional)));

        // Assert
        var elements = GetElements(_builder);
        Assert.That(elements, Has.Count.EqualTo(1));
        Assert.That(elements, Does.Contain(first));
    }

    [Test]
    [Description("AddElements сохраняет ранее добавленные элементы, если новый вызов падает по размерности.")]
    public void AddElements_WhenMixedDimensionsWithinCallAfterPreviousSuccess_KeepsPreviousElements()
    {
        // Arrange
        var existing = CreateTestElement(Dimensional.OneDimensional);
        _builder.AddElements(existing);

        // Act
        Assert.Throws<FeaModelBuilderException>(() => _builder.AddElements(
            CreateTestElement(Dimensional.OneDimensional),
            CreateTestElement(Dimensional.TwoDimensional)));

        // Assert
        var elements = GetElements(_builder);
        Assert.That(elements, Has.Count.EqualTo(1));
        Assert.That(elements, Does.Contain(existing));
    }

    #endregion

    #region AddElements — успешные сценарии

    [Test]
    [Description("AddElements должен возвращать тот же экземпляр builder для fluent-цепочек.")]
    public void AddElements_WithMatchingDimension_ReturnsSameBuilderInstance()
    {
        // Arrange
        var element = CreateTestElement(Dimensional.OneDimensional);

        // Act
        var result = _builder.AddElements(element);

        // Assert
        Assert.That(result, Is.SameAs(_builder));
    }

    [TestCase(Dimensional.OneDimensional, TestName = "AddElements with 1D elements succeeds")]
    [TestCase(Dimensional.TwoDimensional, TestName = "AddElements with 2D elements succeeds")]
    [TestCase(Dimensional.ThreeDimensional, TestName = "AddElements with 3D elements succeeds")]
    [Description("AddElements должен успешно принимать элементы любой размерности, если они согласованы.")]
    public void AddElements_WithConsistentDimension_DoesNotThrow(Dimensional dimension)
    {
        Assert.DoesNotThrow(
            () => _builder.AddElements(CreateTestElement(dimension)));
    }

    [Test]
    [Description("AddElements должен добавить все элементы из коллекции.")]
    public void AddElements_WithMultipleMatchingElements_AddsAll()
    {
        // Arrange
        var first = CreateTestElement(Dimensional.OneDimensional);
        var second = CreateTestElement(Dimensional.OneDimensional);
        var third = CreateTestElement(Dimensional.OneDimensional);

        // Act
        _builder.AddElements(first, second, third);

        // Assert
        Assert.That(GetElements(_builder), Has.Count.EqualTo(3));
    }

    [Test]
    [Description("Несколько вызовов AddElements с одинаковой размерностью накапливают элементы.")]
    public void AddElements_WhenCalledMultipleTimes_AccumulatesElements()
    {
        // Arrange
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
    [Description("Регрессия: повторный AddElements не должен бросать 'change node type' из SetNodesType.")]
    public void AddElements_WhenCalledMultipleTimesWithSameDimension_DoesNotThrowNodeTypeChangeError()
    {
        // Arrange
        _builder.AddElements(CreateTestElement(Dimensional.OneDimensional));

        // Act & Assert
        Assert.DoesNotThrow(() =>
            _builder.AddElements(CreateTestElement(Dimensional.OneDimensional)));
    }

    #endregion

    #region Helpers

    private static ConstructionModelBuilder CreateBuilderWithElements(
        Dimensional dimension, params Freedom[] freedoms)
    {
        var builder = new ConstructionModelBuilder();
        builder.AddElements(CreateTestElement(dimension, freedoms));
        return builder;
    }

    private static IFiniteElement CreateTestElement(Dimensional dimension, params Freedom[] freedoms)
    {
        var builder = new FiniteElementBuilder();

        foreach (var freedom in freedoms)
        {
            builder.AddFreedom(freedom, new TemperatureElementOptions
            {
                ThermalConductivity = 1,
                Thickness = 1
            });
        }

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

    private static IFiniteElement CreateTestElement(Dimensional dimension)
        => CreateTestElement(dimension, Freedom.Temperature);

    private static ICollection<IFiniteElement> GetElements(ConstructionModelBuilder builder)
    {
        var property = typeof(ConstructionModelBuilder)
            .GetProperty("Elements", BindingFlags.NonPublic | BindingFlags.Instance);

        return (ICollection<IFiniteElement>)property!.GetValue(builder)!;
    }

    /// <summary>
    /// Устанавливает приватное свойство NodeType через рефлексию.
    /// Нужно для сценария «NodeType задан, но элементов нет» — публичного API для этого нет.
    /// </summary>
    private static void SetNodeTypeViaReflection(ConstructionModelBuilder builder, ElementNodeType nodeType)
    {
        var property = typeof(ConstructionModelBuilder)
            .GetProperty("NodeType", BindingFlags.NonPublic | BindingFlags.Instance);

        property!.SetValue(builder, nodeType);
    }

    private static ElementNodeType CreateNodeType(Dimensional dimension) =>
        dimension switch
        {
            Dimensional.OneDimensional => ElementNodeType.Type1D,
            Dimensional.TwoDimensional => ElementNodeType.Type2D,
            Dimensional.ThreeDimensional => ElementNodeType.Type3D,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null)
        };

    #endregion
}