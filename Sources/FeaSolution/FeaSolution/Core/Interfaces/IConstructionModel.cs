using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface IConstructionModel
{
    /// <summary>
    /// Допустимый тип узлов модели конструкции.
    /// </summary>
    ElementNodeType NodeType { get; }

    /// <summary>
    /// Коллекция конечных элементов модели конструкции.
    /// </summary>
    ICollection<IFiniteElement> Elements { get; }
}