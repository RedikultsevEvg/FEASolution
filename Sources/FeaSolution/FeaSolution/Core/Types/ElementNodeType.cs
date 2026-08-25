using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

/// <summary>
/// Тип узла конечного элемента.
/// </summary>
public class ElementNodeType
{
    /// <summary>
    /// Размерность узла.
    /// </summary>
    public Dimensional Dimension { get; init; }
}

