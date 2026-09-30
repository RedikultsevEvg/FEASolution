using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public sealed class SmallStiffnessMatrix : IStiffnessMatrix
{
    private readonly IElementNode[] _nodes;
    
    // packed upper triangle
    private readonly MatrixValue[] _matrixData; 

    internal int NodeCount => Nodes.Count;

    internal MatrixValue DefaultValue { get; } = 0.0;

    /// <summary>
    /// Создаёт маленькую симметричную матрицу фиксированного размера.
    /// </summary>
    /// <param name="nodes">Узлы в порядке индексации.</param>
    public SmallStiffnessMatrix(IReadOnlyList<IElementNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count == 0)
            throw new ArgumentException("Nodes cannot be empty.", nameof(nodes));

        _nodes = [.. nodes];
        var n = _nodes.Length;
        _matrixData = new MatrixValue[n * (n + 1) / 2];
    }

    public IReadOnlyList<IElementNode> Nodes => _nodes;

    public void AddValue(IElementNode firstNode, IElementNode secondNode, MatrixValue valueToAdd)
    {
        throw new NotImplementedException();
    }

    public MatrixValue this[IElementNode firstElement, IElementNode secondElement]
    {
        get
        {
            var a = IndexOf(firstElement);
            var b = IndexOf(secondElement);

            if (a < 0 || b < 0) 
                return DefaultValue;

            return _matrixData[PackIndex(a, b)];
        }
        set
        {
            var a = IndexOf(firstElement);
            var b = IndexOf(secondElement);

            if (a < 0 || b < 0) 
                return;

            _matrixData[PackIndex(a, b)] = value;
        }
    }

    /// <summary>
    /// Обходит ненулевые ячейки верхнего треугольника без аллокаций.
    /// </summary>
    public IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroElements()
    {
        var n = _nodes.Length;

        for (var i = 0; i < n; i++)
            for (var j = i; j < n; j++)
            {
                var value = _matrixData[PackIndex(i, j)];
                if (value != DefaultValue)
                    yield return (_nodes[i], _nodes[j], value);
            }
    }

    /// <summary>
    /// Индекс в упакованном верхнем треугольнике: i <= j.
    /// </summary>
    private int PackIndex(int i, int j)
    {
        if (i > j) (i, j) = (j, i);
        // packed upper triangle, row-major
        return i * _nodes.Length - i * (i - 1) / 2 + (j - i);
    }

    private int IndexOf(IElementNode node)
    {
        // Линейный поиск: для n <= 12 это быстрее словаря.
        var span = _nodes.AsSpan();
        for (var k = 0; k < span.Length; k++)
        {
            if (ReferenceEquals(span[k], node)) return k;
        }
        return -1;
    }
}