using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModels;

///<inheritdoc cref="IConstructionModel"/>
public class ConstructionModel : IConstructionModel
{
    /// <inheritdoc/>
    public required ElementNodeType AllowedNodeType { get; init; }

    /// <inheritdoc/>
    public ICollection<IFiniteElement> Elements { get; } = (List<IFiniteElement>)[];

    public IConstructionModel AddElement(IFiniteElement element)
    {
        ArgumentNullException.ThrowIfNull(element, "The finite element can't be null");

        Elements.Add(element);

        return this;
    }
}