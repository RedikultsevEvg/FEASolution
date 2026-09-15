using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo3D;

internal static class ConstructionModel3DExamples
{
    public static void Build_Empty_ConstructionModel()
    {
        var modelBuilder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D);

        modelBuilder.CreateModel();
    }

    public static void Build_Model_From_Element()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(3.4, 35.9, 5)
            .AddNode(5.7, 22, 7)
            .AddNode(2.5, 3.8, 2)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddElements(element1)
            .CreateModel();
    }

    public static void Build_Model_From_3_Elements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(3.4, 35.9, 5)
            .AddNode(5.7, 22, 7)
            .AddNode(2.5, 3.8, 2)
            .CreateElement();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(2.444, 1, 3.6)
            .AddNode(504.7f, 2, 7.5)
            .AddNode(254.54, 34, 3.9)
            .CreateElement();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(2.444, 1, 3.6)
            .AddNode(504.7f, 2, 7.5)
            .AddNode(254.54, 34, 3.9)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddElements(element1, element2, element3)
            .CreateModel();
    }

    public static void Build_Model_From_CollectionOfElements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(3.4, 35.9, 5)
            .AddNode(5.7, 22, 7)
            .AddNode(2.5, 3.8, 2)
            .CreateElement();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(2.444, 1, 3.6)
            .AddNode(504.7f, 2, 7.5)
            .AddNode(254.54, 34, 3.9)
            .CreateElement();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddNode(2.444, 1, 3.6)
            .AddNode(504.7f, 2, 7.5)
            .AddNode(254.54, 34, 3.9)
            .CreateElement();

        var elementCollection = new [] { element1, element2, element3 };

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type3D)
            .AddElements(elementCollection)
            .CreateModel();
    }
}