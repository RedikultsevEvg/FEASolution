using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// The construction model solution.
/// </summary>
public interface IConstructionModelSolution
{
    /// <summary>
    /// Gets solution item.
    /// </summary>
    SolutionValue GetSolutionValue(IElementNode node, DegreeOfFreedom degreeOfFreedom);
}