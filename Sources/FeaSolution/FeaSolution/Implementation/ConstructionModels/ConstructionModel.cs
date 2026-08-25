using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModels;

public class ConstructionModel : IConstructionModel
{
    public required ElementNodeType NodeType { get; init; }

    public ICollection<IFiniteElement> Elements { get; } = new List<IFiniteElement>();
}