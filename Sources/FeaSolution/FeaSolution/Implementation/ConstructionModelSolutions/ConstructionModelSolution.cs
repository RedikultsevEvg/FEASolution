using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModelSolutions;

/// <inheritdoc />
public class ConstructionModelSolution : IConstructionModelSolution
{

    /// <inheritdoc />
    public SolutionValue GetSolutionValue(IElementNode node, DegreeOfFreedom dof)
    {
        var item = Items.FirstOrDefault(x =>
            x.Node == node && x.DegreeOfFreedom == dof);

        return item?.Value ?? throw new InvalidOperationException(
            $"No solution for node {node} and DOF {dof}");
    }

    public required ICollection<SolutionItem> Items { get; init; }
}