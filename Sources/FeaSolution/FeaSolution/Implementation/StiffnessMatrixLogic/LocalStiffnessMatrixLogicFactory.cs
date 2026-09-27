using FeaSolution.Core.Enums;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public static class LocalStiffnessMatrixLogicFactory
{
    public static ILocalStiffnessMatrixLogic GetLogic(ICollection<Freedom> freedoms, Dimensional dimensional, int nodeCount)
    {
        return new Triangle2DLocalStiffnessMatrixLogic();
    }
}