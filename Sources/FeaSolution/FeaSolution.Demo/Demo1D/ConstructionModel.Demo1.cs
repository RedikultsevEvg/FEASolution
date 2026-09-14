using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo1D;

internal static class ConstructionModel1D
{
    public static void Build_Empty_ConstructionModel()
    {
        var modelBuilder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1d);

        modelBuilder.CreateModel();
    }

    public static void Build_Model_1_Element()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddNode(3.4)
            .AddNode(5.7)
            .AddNode(2.5)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddElements(element1)
            .CreateModel();
    }

    public static void Build_Model_3_Element()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddNode(3.4)
            .AddNode(5.7)
            .AddNode(2.5)
            .CreateElement();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddNode(2.444)
            .AddNode(504.7f)
            .AddNode(254.54)
            .CreateElement();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddNode(2.444)
            .AddNode(504.7f)
            .AddNode(254.54)
            .CreateElement();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1d)
            .AddElements(element1, element2, element3)
            .CreateModel();
    }
}