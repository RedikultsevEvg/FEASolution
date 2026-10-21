using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.FiniteElements;

/// <inheritdoc/>
public class FiniteElement : IFiniteElement
{
    /// <inheritdoc/>
    public string UserId { get; init; } = "";

    /// <inheritdoc/>
    public required FiniteElementType Type { get; init; } 

    /// <inheritdoc/>
    public ICollection<IElementNode> Nodes { get; init; } = [];

    /// <inheritdoc/>
    public IElementNode Node(string nodeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeId);

        var node = Nodes.FirstOrDefault(n => n.NodeId == nodeId);
        FeaCommonException.ThrowIfTrue(node == null, $"Can't find the node with id = '{nodeId}'");

        return node!;
    }
}