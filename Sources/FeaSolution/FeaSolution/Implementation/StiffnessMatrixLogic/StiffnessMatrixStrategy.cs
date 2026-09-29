using FeaSolution.Core.Enums;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Stiffness matrix strategy.
/// </summary>
public static class StiffnessMatrixStrategy
{
    public static IStiffnessMatrixLogic GetLogic(
        ICollection<Freedom> freedoms, 
        Dimensional dimensional, 
        int nodeCount)
    {
        return new StiffnessTriangle2DTemperatureLogic();
    }
}