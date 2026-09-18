using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModels;

///<inheritdoc cref="IConstructionModel"/>
public class ConstructionModel : IConstructionModel
{
    /// <inheritdoc/>
    public required ElementNodeType AllowedNodeType { get; init; }

    /// <inheritdoc/>
    public ICollection<IFiniteElement> Elements { get; init; } = (List<IFiniteElement>)[];

    public ICollection<IElementNode> GetNodes()
    {
        return [.. Elements.SelectMany(element => element.Nodes)];
    }
}