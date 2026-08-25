using FeaSolution.Implementation.Builders;
using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.Demo.Demo1;

internal static class ConstructionModelDemo
{
    public static void BuildConstructionModel()
    {
        var nodeType = ElementNodeTypeBuilder.CreateNew3D();

        var model = new ConstructionModel
        {
            NodeType = nodeType
        };
    }
}