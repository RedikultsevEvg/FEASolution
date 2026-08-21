using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// Узел конечного элемента. Узел может быть одномерным, двумерным или трехмерным.
/// </summary>
public interface IElementNode
{
    /// <summary>
    /// Тип узла конечного элемента. Определяет размерность узла.
    /// </summary>
    ElementNodeType Type { get; }

    /// <summary>
    /// Координата X. 
    /// </summary>
    double X { get; set; }

    /// <summary>
    /// Координата Y. Равна 0 если тип узла OneDimensional.
    /// </summary>
    double Y { get; set; }

    /// <summary>
    /// Координата Z. Равна 0 если тип узла OneDimensional или TwoDimensional.
    /// </summary>
    double Z { get; set; }
}