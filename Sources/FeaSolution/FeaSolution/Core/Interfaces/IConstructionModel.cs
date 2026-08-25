using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// Модель конструкции.
/// </summary>
public interface IConstructionModel
{
    /// <summary>
    /// Допустимый тип узлов модели конструкции.
    /// </summary>
    ElementNodeType AllowedNodeType { get; }

    /// <summary>
    /// Коллекция конечных элементов модели конструкции.
    /// </summary>
    ICollection<IFiniteElement> Elements { get; }
}