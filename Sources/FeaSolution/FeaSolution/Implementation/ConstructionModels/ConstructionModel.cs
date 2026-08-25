using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModels;

/// <summary>
/// Модель конструкции.
/// </summary>
public class ConstructionModel : IConstructionModel
{
    /// <summary>
    /// Допустимый тип элемента.
    /// </summary>
    public required ElementNodeType AllowedNodeType { get; init; }

    /// <summary>
    /// Коллекция конечных элементов.
    /// </summary>
    public ICollection<IFiniteElement> Elements { get; } = (List<IFiniteElement>)[];
}