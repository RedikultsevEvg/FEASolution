using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.DirichletCalculation;

/// <summary>
/// System of linear equations logic.
/// </summary>
internal interface IDirichletLogic
{
    /// <summary>
    /// Applies the Dirichle condition to matrix K and vector F. <br/>
    /// K * T = F
    /// </summary>
    /// <param name="matrixK">Matrix K.</param>
    /// <param name="vectorF">Vector F.</param>
    /// <param name="dirichleConditions">Collection of Direchle conditions.</param>
    void ApplyDirichlet(IStiffnessMatrix matrixK,
        SolutionItem[] vectorF, ICollection<DirichleCondition> dirichleConditions);
}