using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo2D;

internal static class ConstructionModel2DExamples
{
    public static void Build_Empty_ConstructionModel()
    {
        var modelBuilder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D);

        modelBuilder.CreateModel();
    }

    public static void Build_Model_From_Element()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(3.4, 35.9)
            .AddNode(5.7, 22)
            .AddNode(2.5, 3.8)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1)
            .CreateModel();
    }

    public static void Build_Model_From_3_Elements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(3.4, 35.9)
            .AddNode(5.7, 22)
            .AddNode(2.5, 3.8)
            .CreateElement();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.444, 1)
            .AddNode(504.7f, 2)
            .AddNode(254.54, 34)
            .CreateElement();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.444, 1)
            .AddNode(504.7f, 2)
            .AddNode(254.54, 34)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2, element3)
            .CreateModel();
    }

    public static void Build_Model_From_CollectionOfElements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(3.4, 35.9)
            .AddNode(5.7, 22)
            .AddNode(2.5, 3.8)
            .CreateElement();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.444, 1)
            .AddNode(504.7f, 2)
            .AddNode(254.54, 34)
            .CreateElement();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.444, 1)
            .AddNode(504.7f, 2)
            .AddNode(254.54, 34)
            .CreateElement();

        var elementCollection = new [] { element1, element2, element3 };

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(elementCollection)
            .CreateModel();
    }
}