using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixs;

public interface IStiffnessMatrix
{
    MatrixValue this[IElementNode firstNode, IElementNode secondNode] { get; set; }

    /// <summary>
    /// Обходит ненулевые ячейки верхнего треугольника.
    /// Каждая симметричная пара (i, j) выдаётся один раз.
    /// </summary>
    IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroElements();

    IReadOnlyList<IElementNode> Nodes { get; }

    void AddValue(IElementNode firstNode, IElementNode secondNode, MatrixValue valueToAdd);
}