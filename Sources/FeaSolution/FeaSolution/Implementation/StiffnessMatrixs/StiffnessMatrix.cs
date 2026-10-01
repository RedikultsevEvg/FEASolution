using FeaSolution.Core.Interfaces;
using System.Runtime.InteropServices;

namespace FeaSolution.Implementation.StiffnessMatrixs;

/// <summary>
/// Stiffness matrix for a large sparse symmetric matrix.
/// </summary>
public sealed class StiffnessMatrix : IStiffnessMatrix
{
    private readonly Dictionary<IElementNode, int> _nodeToIndex =
        new(ReferenceEqualityComparer.Instance);

    private readonly List<IElementNode> _indexToNode = [];

    private readonly Dictionary<long, MatrixValue> _matrixData = [];

    internal MatrixValue DefaultValue { get; } = 0.0;

    public int NodeCount => _nodeToIndex.Count;

    /// <inheritdoc/>>
    public IReadOnlyList<IElementNode> Nodes => _indexToNode;

    /// <inheritdoc/>>
    public void AddValue(IElementNode firstNode, IElementNode secondNode, MatrixValue valueToAdd)
    {
        if (valueToAdd == 0.0) return;

        var ia = GetOrAddIndex(firstNode);
        var ib = GetOrAddIndex(secondNode);
        var key = GetKey(ia, ib);

        ref var matrixValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_matrixData, key, out _);
        matrixValue += valueToAdd;

        if (matrixValue == 0.0)
            _matrixData.Remove(key);
    }

    /// <inheritdoc/>>
    public MatrixValue this[IElementNode firstNode, IElementNode secondNode]
    {
        get
        {
            if (!_nodeToIndex.TryGetValue(firstNode, out var a) ||
                !_nodeToIndex.TryGetValue(secondNode, out var b))
            {
                return DefaultValue;
            }
            return _matrixData.GetValueOrDefault(GetKey(a, b), DefaultValue);
        }
        set
        {
            if (value == DefaultValue)
            {
                if (!_nodeToIndex.TryGetValue(firstNode, out var a) ||
                    !_nodeToIndex.TryGetValue(secondNode, out var b))
                {
                    return;
                }
                _matrixData.Remove(GetKey(a, b));
                return;
            }

            int ia = GetOrAddIndex(firstNode);
            int ib = GetOrAddIndex(secondNode);
            _matrixData[GetKey(ia, ib)] = value;
        }
    }

    /// <inheritdoc/>>
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