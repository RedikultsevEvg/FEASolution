using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ConstructionModelSolutions;

/// <inheritdoc />
public class ConstructionModelSolution : IConstructionModelSolution
{

    /// <inheritdoc />
    public SolutionValue GetSolutionValue(IElementNode node, Freedom freedom)
    {
        var item = Items.FirstOrDefault(x =>
            x.Node == node && x.Freedom == freedom);

        return item?.Value ?? throw new InvalidOperationException(
            $"No solution for node {node} and DOF {freedom}");
    }

    public required ICollection<SolutionItem> Items { get; init; }
}