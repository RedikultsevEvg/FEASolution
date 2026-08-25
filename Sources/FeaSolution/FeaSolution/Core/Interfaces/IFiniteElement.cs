using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface IFiniteElement
{
    /// <summary>
    /// Уникальный пользовательский идентификатор в рамках решения.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Тип конечного элемента.
    /// </summary>
    FiniteElementType ElementType { get; }

    /// <summary>
    /// Узлы конечного элемента.
    /// </summary>
    IElementNode[] Nodes { get; }

    /// <summary>
    /// Матрица жесткости конечного элемента.
    /// </summary>
    IFiniteElementStiffnessMatrix StiffnessMatrix { get; }
}