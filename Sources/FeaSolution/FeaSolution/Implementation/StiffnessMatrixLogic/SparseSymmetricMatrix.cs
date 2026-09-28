using FeaSolution.Core.Interfaces;
using FeaSolution.Implementation.ElementNodes;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Разреженная симметричная матрица. Каждый ElementNode маппится в int-индекс,
/// ключ хранится как упакованный long (min << 32 | max).
/// Узел регистрируется только при записи ненулевого значения.
/// </summary>
public sealed class SparseSymmetricMatrix
{
    private readonly Dictionary<IElementNode, int> _indexes =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<long, double> _data = [];

    public double DefaultValue { get; } = 0.0;

    public int CountData => _data.Count;

    public int MatrixDimension => _indexes.Count;

    public double this[IElementNode firstElement, IElementNode secondElement]
    {
        get
        {
            // Чтение не регистрирует узлы
            if (!_indexes.TryGetValue(firstElement, out var a) ||
                !_indexes.TryGetValue(secondElement, out var b))
            {
                return DefaultValue;
            }

            return _data.GetValueOrDefault(Key(a, b), DefaultValue);
        }
        set
        {
            if (value == DefaultValue)
            {
                // Удаление: узлы уже могут быть зарегистрированы
                if (!_indexes.TryGetValue(firstElement, out var a) ||
                    !_indexes.TryGetValue(secondElement, out var b))
                {
                    return; // связи и так нет
                }

                _data.Remove(Key(a, b));
                return;
            }

            // Запись ненулевого значения — регистрируем оба узла
            int ia = GetOrAddIndex(firstElement);
            int ib = GetOrAddIndex(secondElement);
            _data[Key(ia, ib)] = value;
        }
    }

    public bool HasRelation(IElementNode i, IElementNode j)
    {
        if (!_indexes.TryGetValue(i, out var a) ||
            !_indexes.TryGetValue(j, out var b))
        {
            return false;
        }
        return _data.ContainsKey(Key(a, b));
    }

    // Упаковка пары индексов в long. Симметрия: сначала min.
    private static long Key(int a, int b)
    {
        if (a > b) (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    // Регистрация узла и выдача индекса
    private int GetOrAddIndex(IElementNode node)
    {
        if (_indexes.TryGetValue(node, out var idx)) return idx;

        idx = _indexes.Count;
        _indexes[node] = idx;
        return idx;
    }
}