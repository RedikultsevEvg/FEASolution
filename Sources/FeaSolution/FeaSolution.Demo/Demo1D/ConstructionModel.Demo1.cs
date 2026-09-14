using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo1D;

internal static class BuildModel
{
    public static void BuildEmptyConstructionModel()
    {
        new ConstructionModelBuilder()
            .CreateModel();
    }

    public static void BuildEmptyConstructionModel1D()
    {
        var modelBuilder = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1d);

        var model = modelBuilder.CreateModel();
    }
}