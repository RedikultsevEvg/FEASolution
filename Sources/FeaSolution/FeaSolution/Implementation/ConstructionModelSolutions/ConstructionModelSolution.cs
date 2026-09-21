using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModelSolutions;

/// <inheritdoc />
public class ConstructionModelSolution : IConstructionModelSolution
{

    /// <inheritdoc />
    public SolutionValue GetSolutionValue(IElementNode node, DegreeOfFreedom degreeOfFreedom) => 0;

    public required ICollection<SolutionItem> Items { get; init; }
}