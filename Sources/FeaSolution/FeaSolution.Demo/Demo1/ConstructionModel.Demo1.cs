using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo1;

internal static class ConstructionModelDemo
{
    public static void BuildConstructionModel()
    {
        var model = ConstructionModelBuilder.CreateNew3D();

    }
}