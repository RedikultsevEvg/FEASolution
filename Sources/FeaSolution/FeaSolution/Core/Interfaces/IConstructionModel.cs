using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface IConstructionModel
{
    /// <summary>
    /// Коллекция конечных элементов модели конструкции.
    /// </summary>
    IFiniteElement[] Elements { get; }

    /// <summary>
    /// Допустимый тип узлов модели конструкции.
    /// </summary>
    ElementNodeType NodeType { get; }
}