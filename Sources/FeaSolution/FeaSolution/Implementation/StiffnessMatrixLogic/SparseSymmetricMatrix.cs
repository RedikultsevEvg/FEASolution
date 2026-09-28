using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Разреженная симметричная матрица. Каждый ElementNode маппится в int-индекс,
/// ключ хранится как упакованный long (min << 32 | max).
/// Узел регистрируется только при записи ненулевого значения.
/// </summary>
public sealed class SparseSymmetricMatrix
{
    private readonly Dictionary<IElementNode, int> _nodes =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<long, MatrixValue> _matrixData = [];

    public MatrixValue DefaultValue { get; } = 0.0;

    public int NoneZeroElementCount => _matrixData.Count;

    public int NodeCount => _nodes.Count;

    public MatrixValue this[IElementNode firstElement, IElementNode secondElement]
    {
        get
        {
            if (!_nodes.TryGetValue(firstElement, out var a) ||
                !_nodes.TryGetValue(secondElement, out var b))
            {
                return DefaultValue;
            }

            return _matrixData.GetValueOrDefault(GetKey(a, b), DefaultValue);
        }
        set
        {
            if (value == DefaultValue)
            {
                // Удаление: узлы уже могут быть зарегистрированы
                if (!_nodes.TryGetValue(firstElement, out var a) ||
                    !_nodes.TryGetValue(secondElement, out var b))
                {
                    return; // связи и так нет
                }

                _matrixData.Remove(GetKey(a, b));
                return;
            }

            // Запись ненулевого значения — регистрируем оба узла
            int ia = GetOrAddIndex(firstElement);
            int ib = GetOrAddIndex(secondElement);
            _matrixData[GetKey(ia, ib)] = value;
        }
    }

    public bool HasRelation(IElementNode i, IElementNode j)
    {
        if (!_nodes.TryGetValue(i, out var a) ||
            !_nodes.TryGetValue(j, out var b))
        {
            return false;
        }
        return _matrixData.ContainsKey(GetKey(a, b));
    }

    /// <summary>
    /// Get key for the dictionary. Min/Max format. 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static long GetKey(int a, int b)
    {
        if (a > b) (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    // Регистрация узла и выдача индекса
    private int GetOrAddIndex(IElementNode node)
    {
        if (_nodes.TryGetValue(node, out var idx)) return idx;

        idx = _nodes.Count;
        _nodes[node] = idx;
        return idx;
    }
}