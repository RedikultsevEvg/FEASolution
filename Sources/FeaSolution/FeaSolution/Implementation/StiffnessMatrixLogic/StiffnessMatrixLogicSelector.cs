using FeaSolution.Core.Enums;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Stiffness matrix logic selector.
/// </summary>
public static class StiffnessMatrixLogicSelector
{
    /// <summary>
    /// Gets the relevant logic.
    /// </summary>
    /// <param name="freedoms"></param>
    /// <param name="dimensional"></param>
    /// <param name="nodeCount"></param>
    /// <returns></returns>
    public static IStiffnessMatrixLogic GetLogic(
        ICollection<Freedom> freedoms, 
        Dimensional dimensional, 
        int nodeCount)
    {
        return new StiffnessTriangle2DTemperatureLogic();
    }
}