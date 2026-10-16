using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Stiffness matrix calculation logic.
/// </summary>
public interface IStiffnessMatrixLogic
{
    /// <summary>
    /// Gets the stiffness matrix.
    /// </summary>
    /// <param name="element">The finite element.</param>
    IStiffnessMatrix GetMatrix(IFiniteElement element);
}