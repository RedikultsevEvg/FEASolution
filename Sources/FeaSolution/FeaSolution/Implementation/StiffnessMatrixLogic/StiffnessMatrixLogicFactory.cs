using FeaSolution.Core.Enums;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

/// <summary>
/// Stiffness Matrix Logic Factory.
/// </summary>
public static class StiffnessMatrixLogicFactory
{
    public static ILocalStiffnessMatrixLogic GetLogic(ICollection<Freedom> freedoms, Dimensional dimensional, int nodeCount)
    {
        return new Triangle2DLocalSparceSymmetricMatrixLogic();
    }
}