namespace FeaSolution.Implementation.StiffnessMatrixLogic;

internal static class StiffnessMatrixExtensions
{
    public static void AddMatrix(
        this StiffnessMatrix globalStiffnessMatrix,
        StiffnessMatrix localMatrix)
    {
        ArgumentNullException.ThrowIfNull(globalStiffnessMatrix);
        ArgumentNullException.ThrowIfNull(localMatrix);

        foreach (var (nodeI, nodeJ, value) in localMatrix.NonZeroElements())
        {
            globalStiffnessMatrix[nodeI, nodeJ] += value;
        }
    }
}