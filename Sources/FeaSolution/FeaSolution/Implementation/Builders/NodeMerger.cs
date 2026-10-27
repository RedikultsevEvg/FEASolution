using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.Builders;

internal static class NodeMerger
{
    public static void MergeEqualNodes(IEnumerable<IFiniteElement> elements, CoordinateValue delta)
    {
        ArgumentNullException.ThrowIfNull(elements);

        FeaModelBuilderException.ThrowIfTrue(delta < 0,
            "The merge delta should be non-negative.");

        var elementList = elements as IList<IFiniteElement> ?? elements.ToList();

        // Собираем все вхождения узлов. Это единственный полный обход всех
        // элементов и их узлов.
        var occurrences = new List<(IFiniteElement Element, IElementNode Node)>();
        foreach (var element in elementList)
        {
            foreach (var node in element.Nodes)
            {
                occurrences.Add((element, node));
            }
        }

        FeaModelBuilderException.ThrowIfTrue(occurrences.Count == 0,
            "There are no nodes to merge.");

        // DSU по вхождениям. Рёбра добавляем только для реальных совпадений
        // (а не для всех пар), поэтому сложность ~ O(N * k), где k — число
        // кандидатов в соседних бакетах.
        var parent = new int[occurrences.Count];
        for (var i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        // elementsByRoot[root] — множество элементов, чьи вхождения уже
        // объединены в группу с данным корнем. Нужно, чтобы не сливать
        // узлы внутри одного элемента.
        var elementsByRoot = new Dictionary<int, HashSet<IFiniteElement>>();

        HashSet<IFiniteElement> GetElements(int root)
        {
            if (!elementsByRoot.TryGetValue(root, out var set))
            {
                set = new HashSet<IFiniteElement>(ReferenceEqualityComparer.Instance);
                elementsByRoot[root] = set;
            }
            return set;
        }

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra == rb)
            {
                return;
            }

            var setA = GetElements(ra);
            var setB = GetElements(rb);

            // Не объединяем два вхождения из одного и того же элемента:
            // пользователь явно потребовал не сливать узлы внутри элемента.
            // (Если a и b из одного элемента — пропускаем.)
            if (ReferenceEquals(occurrences[a].Element, occurrences[b].Element))
            {
                return;
            }

            // Оба корня уже содержат элементы. Если множества пересекаются
            // по элементам — объединение допустимо: в каждом элементе всё
            // равно останется только один экземпляр после замены.
            parent[rb] = ra;
            setA.UnionWith(setB);
            elementsByRoot.Remove(rb);
        }

        // Пространственный индекс: бакет -> список индексов вхождений.
        var buckets = new Dictionary<BucketKey, List<int>>();

        for (var i = 0; i < occurrences.Count; i++)
        {
            var node = occurrences[i].Node;

            // Ищем кандидатов в соседних бакетах.
            foreach (var otherIndex in EnumerateCandidates(buckets, node, delta))
            {
                var otherNode = occurrences[otherIndex].Node;
                if (AreCoordinatesEqual(node, otherNode, delta))
                {
                    Union(i, otherIndex);
                }
            }

            // Добавляем текущий узел в индекс — теперь он «проверенный»
            // и может стать общим для последующих.
            var key = BucketKey.From(node, delta);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<int>();
                buckets[key] = list;
            }
            list.Add(i);
        }

        // Собираем группы по корням.
        var groups = new Dictionary<int, List<(IFiniteElement Element, IElementNode Node)>>();
        for (var i = 0; i < occurrences.Count; i++)
        {
            var root = Find(i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<(IFiniteElement, IElementNode)>();
                groups[root] = list;
            }
            list.Add(occurrences[i]);
        }

        // Заменяем ссылки на общий узел в каждой группе.
        foreach (var group in groups.Values)
        {
            if (group.Count == 1)
            {
                continue;
            }

            var sharedNode = ResolveSharedNode(group.Select(g => g.Node).ToArray());

            foreach (var (element, node) in group)
            {
                if (!ReferenceEquals(node, sharedNode))
                {
                    ReplaceNodeInstance(element, node, sharedNode);
                }
            }
        }
    }

    // ---------------------------------------------------------------------
    // Пространственный индекс
    // ---------------------------------------------------------------------

    private readonly record struct BucketKey(long X, long Y, long Z)
    {
        public static BucketKey From(IElementNode node, double delta)
        {
            // delta == 0 → точная координата; иначе — floor(coord / delta).
            // Для delta > 0 используем именно деление на delta, чтобы
            // точки в пределах delta попадали в бакеты, различающиеся
            // максимум на 1 по каждой оси.
            if (delta > 0)
            {
                return new BucketKey(
                    (long)Math.Floor(node.X / delta),
                    (long)Math.Floor(node.Y / delta),
                    (long)Math.Floor(node.Z / delta));
            }

            // Точное совпадение: используем битовое представление double,
            // чтобы не терять точность и не полагаться на Equals.
            return new BucketKey(
                BitConverter.DoubleToInt64Bits(node.X),
                BitConverter.DoubleToInt64Bits(node.Y),
                BitConverter.DoubleToInt64Bits(node.Z));
        }
    }

    private static IEnumerable<int> EnumerateCandidates(
        Dictionary<BucketKey, List<int>> buckets,
        IElementNode node,
        double delta)
    {
        if (delta > 0)
        {
            var key = BucketKey.From(node, delta);
            for (var dx = -1L; dx <= 1L; dx++)
            {
                for (var dy = -1L; dy <= 1L; dy++)
                {
                    for (var dz = -1L; dz <= 1L; dz++)
                    {
                        if (buckets.TryGetValue(
                                new BucketKey(key.X + dx, key.Y + dy, key.Z + dz),
                                out var list))
                        {
                            foreach (var idx in list)
                            {
                                yield return idx;
                            }
                        }
                    }
                }
            }
            yield break;
        }

        // delta == 0 — только точное совпадение координат.
        var exact = BucketKey.From(node, 0);
        if (buckets.TryGetValue(exact, out var exactList))
        {
            foreach (var idx in exactList)
            {
                yield return idx;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Выбор общего узла и замена ссылок (без изменений)
    // ---------------------------------------------------------------------

    private static IElementNode ResolveSharedNode(IReadOnlyCollection<IElementNode> group)
    {
        var nodesWithId = group
            .Where(n => !string.IsNullOrEmpty(n.NodeId))
            .Distinct()
            .ToArray();

        FeaModelBuilderException.ThrowIfTrue(nodesWithId.Length > 1,
            $"Found {nodesWithId.Length} equal nodes with non-empty NodeId " +
            $"({string.Join(", ", nodesWithId.Select(n => $"'{n.NodeId}'"))}). " +
            "Only one node in a group of equal nodes may have a non-empty NodeId.");

        if (nodesWithId.Length == 1)
        {
            return nodesWithId[0];
        }

        return group.First();
    }

    private static void ReplaceNodeInstance(
        IFiniteElement element,
        IElementNode node,
        IElementNode sharedNode)
    {
        var currentNodes = element.Nodes.ToArray();

        var index = -1;
        for (var i = 0; i < currentNodes.Length; i++)
        {
            if (ReferenceEquals(currentNodes[i], node))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        currentNodes[index] = sharedNode;

        element.Nodes.Clear();
        foreach (var n in currentNodes)
        {
            element.Nodes.Add(n);
        }
    }

    private static bool AreCoordinatesEqual(IElementNode a, IElementNode b, CoordinateValue delta)
    {
        return Math.Abs(a.X - b.X) <= delta
               && Math.Abs(a.Y - b.Y) <= delta
               && Math.Abs(a.Z - b.Z) <= delta;
    }
}