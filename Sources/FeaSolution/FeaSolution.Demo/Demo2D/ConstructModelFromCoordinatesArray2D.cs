using FeaSolution.Core.Enums;
using FeaSolution.Demo.Common;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo2D;

internal static class ConstructModelFromCoordinatesArray2D
{
    public static void Get_Solution_From_Coordinates_Array_Example()
    {
        const int elementCount = 10;
        var coordinatesProvider = DemoDataFactory.CreateTrianglesCoordinates(elementCount);

        var modelBuilder = new ConstructionModelBuilder();

        for (var elementIndex = 0; elementIndex < coordinatesProvider.Triangles.Count; elementIndex++)
        {
            var triangleCoordinates = coordinatesProvider.Triangles[elementIndex];

            var elementBuilder = new FiniteElementBuilder()
                .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
                {
                    ThermalConductivity = 1.0,
                    Thickness = 1.0,
                });

            elementBuilder.AddNode(triangleCoordinates.Node1.X, triangleCoordinates.Node1.Y, elementIndex == 0 ? "FirstNode_WithDirichle" : "");
            elementBuilder.AddNode(triangleCoordinates.Node2.X, triangleCoordinates.Node2.Y);
            elementBuilder.AddNode(triangleCoordinates.Node3.X, triangleCoordinates.Node3.Y, elementIndex == elementCount - 1 ? "LastNode_WithDirichle" : "");

            var element = elementBuilder.Build($"The triangle #{elementIndex}");
            modelBuilder
                .AddElements(element);
        }

        var model = modelBuilder
            .Merge() // объединяет совпадающие узлы соседних элементов
            .Build();

        var solution = new SolutionBuilder(model)
            .Assembly()
            .ValidateForCountOfElement()
            .ValidateForCommonNodes()
            .ValidateAll()
            .AddDirichlet("FirstNode_WithDirichle", Freedom.Temperature, 20.0)
            .AddDirichlet("LastNode_WithDirichle", Freedom.Temperature, 100.0)
            .Build();

        DemoAssistant.ConsoleWriteExampleResults(
            nameof(Get_Solution_From_Coordinates_Array_Example),
            solution);
    }
}