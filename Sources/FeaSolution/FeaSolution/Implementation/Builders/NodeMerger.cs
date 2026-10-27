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

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
            {
                parent[rb] = ra;
            }
        }

        for (var i = 0; i < occurrences.Count; i++)
        {
            for (var j = i + 1; j < occurrences.Count; j++)
            {
                // Не сливаем узлы внутри одного элемента.
                if (ReferenceEquals(occurrences[i].Element, occurrences[j].Element))
                {
                    continue;
                }

                if (AreCoordinatesEqual(occurrences[i].Node, occurrences[j].Node, delta))
                {
                    Union(i, j);
                }
            }
        }

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

        foreach (var group in groups.Values)
        {
            // Группа из одного вхождения — менять нечего.
            if (group.Count == 1)
            {
                continue;
            }

            var sharedNode = ResolveSharedNode(group.Select(g => g.Node).ToArray());

            // Заменяем конкретные вхождения из группы, а не все узлы элемента.
            foreach (var (element, node) in group)
            {
                if (!ReferenceEquals(node, sharedNode))
                {
                    ReplaceNodeInstance(element, node, sharedNode);
                }
            }
        }
    }

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

        // Ни у одного узла нет NodeId — используем первый узел группы как общий,
        // не создаём новый экземпляр (см. тест NoNodesWithId_DoNotCreatesNewSharedNode).
        return group.First();
    }

    /// <summary>
    /// Replaces the first occurrence of <paramref name="node"/> (by reference) in
    /// <paramref name="element"/>'s node collection with <paramref name="sharedNode"/>.
    /// Other equal-by-coordinates nodes in the same element are left untouched,
    /// so nodes inside a single element are not merged with each other.
    /// </summary>
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