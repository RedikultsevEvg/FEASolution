using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.FiniteElements;

/// <inheritdoc/>
public class FiniteElement(string userId, FiniteElementType elementType) : IFiniteElement
{
    /// <inheritdoc/>
    public string UserId { get; } = userId;

    /// <inheritdoc/>
    public FiniteElementType ElementType { get; } = elementType;

    /// <inheritdoc/>
    public IElementNode[] Nodes { get; }

    /// <inheritdoc/>
    public IFiniteElementStiffnessMatrix StiffnessMatrix { get; }
}