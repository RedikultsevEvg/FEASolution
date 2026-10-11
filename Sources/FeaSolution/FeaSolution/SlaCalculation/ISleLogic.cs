using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.SlaCalculation;

/// <summary>
/// System of linear equations logic.
/// </summary>
internal interface ISleLogic
{
    /// <summary>
    /// Applies the Dirichle condition to matrix.
    /// </summary>
    /// <param name="globalStiffnessMatrix">Stiffness matrix.</param>
    /// <param name="dirichleConditions">Collection of Direchle conditions.</param>
    void ApplyDirichlet(IStiffnessMatrix globalStiffnessMatrix, ICollection<DirichleCondition> dirichleConditions);
}
