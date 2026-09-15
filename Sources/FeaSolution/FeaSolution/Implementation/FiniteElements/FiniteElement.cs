using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.FiniteElements;

/// <inheritdoc/>
public class FiniteElement : IFiniteElement
{
    /// <inheritdoc/>
    public string UserId { get; init; } = "";

    /// <inheritdoc/>
    public required FiniteElementType ElementType { get; init; } 

    /// <inheritdoc/>
    public ICollection<IElementNode> Nodes { get; init; } = [];

    /// <inheritdoc/>
    public IFiniteElementStiffnessMatrix StiffnessMatrix { get; }
}