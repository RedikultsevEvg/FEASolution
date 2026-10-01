using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixs;

/// <summary>
/// Stiffness matrix for a small packed symmetric matrix.
/// </summary>
public sealed class SmallStiffnessMatrix : IStiffnessMatrix
{
    private const int MaxNodeCount = 12;

    private readonly IElementNode[] _nodes;
    
    // packed upper triangle
    private readonly MatrixValue[] _matrixData; 

    internal int NodeCount => Nodes.Count;

    internal MatrixValue DefaultValue { get; } = 0.0;

    /// <summary>
    /// Creates a small fixed-size symmetric matrix.
    /// </summary>
    /// <param name="nodes">Nodes in indexing order.</param>
    public SmallStiffnessMatrix(IReadOnlyList<IElementNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        FeaCommonException.ThrowIfTrue(nodes.Count == 0, "Nodes cannot be empty.");
        FeaCommonException.ThrowIfTrue(nodes.Count > MaxNodeCount, $"{nameof(SmallStiffnessMatrix)} supports at most {MaxNodeCount} nodes.");

        _nodes = [.. nodes];
        var n = _nodes.Length;
        _matrixData = new MatrixValue[n * (n + 1) / 2];
    }

    /// <inheritdoc/>
    public IReadOnlyList<IElementNode> Nodes => _nodes;

    /// <inheritdoc/>
    public void AddValue(IElementNode firstNode, IElementNode secondNode, MatrixValue valueToAdd)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public MatrixValue this[IElementNode firstNode, IElementNode secondNode]
    {
        get
        {
            var a = IndexOf(firstNode);
            var b = IndexOf(secondNode);

            if (a < 0 || b < 0) 
                return DefaultValue;

            return _matrixData[PackIndex(a, b)];
        }
        set
        {
            var a = IndexOf(firstNode);
            var b = IndexOf(secondNode);

            if (a < 0 || b < 0) 
                return;

            _matrixData[PackIndex(a, b)] = value;
        }
    }

    /// <summary>
    /// Sets a value to the cell at the given node indices.
    /// Symmetric: <paramref name="firstNodeIndex"/> and <paramref name="secondNodeIndex"/>
    /// are interchangeable — the value goes into the packed upper triangle.
    /// </summary>
    /// <param name="firstNodeIndex">Index of the first node in <see cref="Nodes"/>.</param>
    /// <param name="secondNodeIndex">Index of the second node in <see cref="Nodes"/>.</param>
    /// <param name="valueToAdd">Value to set to the cell.</param>
    public void SetValue(int firstNodeIndex, int secondNodeIndex, MatrixValue valueToAdd)
    {
        FeaCommonException.ThrowIfTrue(
            firstNodeIndex >= _nodes.Length || secondNodeIndex >= _nodes.Length,"One of index is out of range" );

        _matrixData[PackIndex(firstNodeIndex, secondNodeIndex)] = valueToAdd;
    }

    // todo: в целях ускорения переделать в возврат без использования yield
    /// <summary>
    /// Обходит ненулевые ячейки верхнего треугольника без аллокаций.
    /// </summary>
    public IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroMatrixValues()
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