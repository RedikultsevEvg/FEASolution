using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.Builders;

namespace FeaSolution.Demo.Demo1D;

internal static class Model1DExamples
{
    public static void Build_Model_From_Element()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 3.5,
                Thickness = 2.8,
            })
            .AddNode(3.4)
            .AddNode(5.7)
            .AddNode(2.5)
            .Build();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddElements(element1)
            .Build();
    }

    public static void Build_Model_From_3_Elements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 3.5, 
                Thickness = 2.8,
            })
            .AddNode(3.4)
            .AddNode(5.7)
            .AddNode(2.5)
            .Build();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(2.444)
            .AddNode(504.7f)
            .AddNode(254.54)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 4,
                Thickness = 0.5,
            })
            .Build();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(2.444)
            .AddNode(504.7f)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 0.06,
            })
            .AddNode(254.54)
            .Build();

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddElements(element1, element2, element3)
            .Build();
    }

    public static void Build_Model_From_CollectionOfElements()
    {
        var element1 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(3.4)
            .AddNode(5.7)
            .AddNode(2.5)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 0.06,
            })
            .Build();

        var element2 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(2.444)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 0.06,
            })
            .AddNode(504.7f)
            .AddNode(254.54)
            .Build();

        var element3 = new FiniteElementBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddNode(2.444)
            .AddNode(504.7f)
            .AddFreedom(Freedom.Temperature, new TemperatureElementOptions
            {
                ThermalConductivity = 0.67,
                Thickness = 0.06,
            })
            .AddNode(254.54)
            .Build();

        var elementCollection = new [] { element1, element2, element3 };

        new ConstructionModelBuilder()
            .SetNodesType(ElementNodeType.Type1D)
            .AddElements(elementCollection)
            .Build();
    }
}