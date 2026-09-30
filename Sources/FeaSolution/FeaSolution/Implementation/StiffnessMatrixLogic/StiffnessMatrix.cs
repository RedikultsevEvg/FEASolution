using FeaSolution.Core.Interfaces;
using System.Runtime.InteropServices;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public sealed class StiffnessMatrix : IStiffnessMatrix
{
    private readonly Dictionary<IElementNode, int> _nodeToIndex =
        new(ReferenceEqualityComparer.Instance);

    private readonly List<IElementNode> _indexToNode = [];

    private readonly Dictionary<long, MatrixValue> _matrixData = [];

    private readonly Lock _addValue = new();

    internal MatrixValue DefaultValue { get; } = 0.0;

    public int NodeCount => _nodeToIndex.Count;

    /// <summary>Зарегистрированные узлы в порядке регистрации.</summary>
    public IReadOnlyList<IElementNode> Nodes => _indexToNode;

    public void AddValue(IElementNode firstElement, IElementNode secondElement, MatrixValue valueToAdd)
    {
        if (valueToAdd == 0.0) return;

        lock (_addValue)
        {
            var ia = GetOrAddIndex(firstElement);
            var ib = GetOrAddIndex(secondElement);
            var key = GetKey(ia, ib);

            ref var matrixValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_matrixData, key, out _);

            matrixValue += valueToAdd;

            if (matrixValue == 0.0)
                _matrixData.Remove(key);
        }
    }

    public MatrixValue this[IElementNode firstElement, IElementNode secondElement]
    {
        get
        {
            if (!_nodeToIndex.TryGetValue(firstElement, out var a) ||
                !_nodeToIndex.TryGetValue(secondElement, out var b))
            {
                return DefaultValue;
            }
            return _matrixData.GetValueOrDefault(GetKey(a, b), DefaultValue);
        }
        set
        {
            if (value == DefaultValue)
            {
                if (!_nodeToIndex.TryGetValue(firstElement, out var a) ||
                    !_nodeToIndex.TryGetValue(secondElement, out var b))
                {
                    return;
                }
                _matrixData.Remove(GetKey(a, b));
                return;
            }

            int ia = GetOrAddIndex(firstElement);
            int ib = GetOrAddIndex(secondElement);
            _matrixData[GetKey(ia, ib)] = value;
        }
    }

    /// <summary>
    /// Обходит ненулевые ячейки верхнего треугольника.
    /// Каждая симметричная пара (i, j) выдаётся один раз.
    /// </summary>
    public IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroElements()
    {
        foreach (var (key, value) in _matrixData)
        {
            var a = (int)(key >> 32);
            var b = (int)(key & 0xFFFFFFFF);

            yield return (_indexToNode[a], _indexToNode[b], value);
        }
    }

    private static long GetKey(int a, int b)
    {
        if (a > b) (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    private int GetOrAddIndex(IElementNode node)
    {
        if (_nodeToIndex.TryGetValue(node, out var idx)) return idx;

        idx = _indexToNode.Count;
        _nodeToIndex[node] = idx;
        _indexToNode.Add(node);
        return idx;
    }
}