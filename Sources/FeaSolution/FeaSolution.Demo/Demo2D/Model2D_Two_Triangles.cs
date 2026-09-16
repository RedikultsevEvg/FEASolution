using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo2D;

internal static class Model2D_Two_Triangles
{
    public static void Build_Model_From_Two_Triangles()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0, 1.0)
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .Build("The first triangle");

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .AddNode(4.0, 1.0)
            .Build("The second triangle");

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2)
            .Merge()
            .Build();
    }
}