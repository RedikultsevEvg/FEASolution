namespace FeaSolution.Implementation.Builders;

public class TemperatureElementOptions : IElementOptions
{
    public required double ThermalConductivity { get; set; }

    public required double Thickness { get; set; }
}