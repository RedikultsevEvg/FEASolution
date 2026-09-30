using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Demo.Common;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo2D;

internal static class Model2DTwoTriangles
{
    public static void Get_Solution_For_Two_Triangles_Example()
    {
        var element1 = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 4.4,
            })
            .SetNodesType(ElementNodeType.Type2D)
            .AddNode(0, 1.0)
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .Build("The first triangle");

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 5.6,
            })
            .AddNode(2.0, 2.0)
            .AddNode(2.0, 0)
            .AddNode(4.0, 1.0)
            .Build("The second triangle");

        var model = new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type2D)
            .AddElements(element1, element2)
            .Merge()
            .Build();

        var solution = new SolutionBuilder(model)
            .AssemblyAsync()
            .Result
            .ValidateForCountOfElement()
            .ValidateForCommonElements()
            .ValidateForQualityOfElements()
            .ValidateForZeroSquareElements()
            .Build();

        DemoAssistant.ConsoleWriteExampleResults(nameof(Get_Solution_For_Two_Triangles_Example), solution);
    }
}