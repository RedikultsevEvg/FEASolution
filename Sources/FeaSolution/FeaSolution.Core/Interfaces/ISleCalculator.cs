using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

/// <summary>
/// SLE calculator.
/// </summary>
public interface ISleCalculator
{
    /// <summary>
    /// Solve the SLE: K * T = F.<br/>
    /// </summary>
    /// <param name="matrixKWithDirichlet">K - matrix NxN.</param>
    /// <param name="vectorFWithDirichlet">F - vector N.</param>
    /// <returns>Vector T - the solution.</returns>
    SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet);
}