using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public interface IStiffnessMatrix
{
    MatrixValue this[IElementNode firstElement, IElementNode secondElement] { get; set; }

    /// <summary>
    /// Обходит ненулевые ячейки верхнего треугольника.
    /// Каждая симметричная пара (i, j) выдаётся один раз.
    /// </summary>
    IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroElements();

    IReadOnlyList<IElementNode> Nodes { get; }

    void AddValue(IElementNode firstElement, IElementNode secondElement, MatrixValue valueToAdd);
}