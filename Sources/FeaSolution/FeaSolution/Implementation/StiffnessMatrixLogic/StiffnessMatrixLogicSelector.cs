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
        if (freedoms.Count == 1 
            && freedoms.ElementAt(0) == Freedom.Temperature
            && dimensional == Dimensional.TwoDimensional
            && nodeCount == 3
            ) return new StiffnessMatrixTriangle2DTemperatureLogic();

        throw new NotSupportedException("The logic for element is not supported");
    }
}