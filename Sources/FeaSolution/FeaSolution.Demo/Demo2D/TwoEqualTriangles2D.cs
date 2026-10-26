using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Demo.Common;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo2D;

internal static class TwoEqualTriangles2D
{
    public static void Get_Solution_For_Two_Triangles_Example()
    {
        var element1 = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(0, 1.0, "nodeA")
            .AddNode(2.0, 2.0, "nodeB")
            .AddNode(2.0, 0, "nodeC")
            .Build("The first triangle");

        var element2 = new FiniteElementBuilder()
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 1.0,
                Thickness = 1.0,
            })
            .AddNode(element1.Node("nodeB"))
            .AddNode(element1.Node("nodeC"))
            .AddNode(4.0, 1.0, "nodeD")
            .Build("The second triangle");

        var model = new ConstructionModelBuilder()
            .AddElements(element1, element2)
            .Merge() // Not implemented yet  - exception if 2
            .Build();

        // var zeroElements = model.GetZeroSquareElements(); // Not implemented yet
        // var notQualityElements = model.GetNotQualityOfElements(); // Not implemented yet

        // 
        // var konturNodes = model.GetNodesFromElements(3);

        var solution = new SolutionBuilder(model)
            .Assembly() 
            .ValidateForCountOfElement() 
            .ValidateForCommonElements() // Not implemented yet 
            .ValidateForQualityOfElements() // Not implemented yet
            .ValidateForZeroSquareElements() // Not implemented yet
            .ValidateAll() // Not implemented yet
            .AddDirichlet("nodeA", Freedom.Temperature, 20.0)
            .AddDirichlet("nodeD", Freedom.Temperature, 100.0)
            .Build();

        DemoAssistant.ConsoleWriteExampleResults(nameof(Get_Solution_For_Two_Triangles_Example), solution);
    }
}