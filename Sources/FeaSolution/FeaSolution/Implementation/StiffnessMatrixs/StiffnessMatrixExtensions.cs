using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixs;

internal static class StiffnessMatrixExtensions
{
    public static void AddMatrix(
        this IStiffnessMatrix globalStiffnessMatrix,
        IStiffnessMatrix localMatrix)
    {
        ArgumentNullException.ThrowIfNull(globalStiffnessMatrix);
        ArgumentNullException.ThrowIfNull(localMatrix);

        foreach (var (nodeI, nodeJ, value) in localMatrix.NonZeroMatrixValues())
        {
            globalStiffnessMatrix.AddValue(nodeI, nodeJ, value);
        }
    }

    // todo: reimplement
    public static bool ValidateMatrixIsSymmetric(this IStiffnessMatrix matrix, MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var nodes = matrix.Nodes;

        for (var i = 0; i < 3; i++)
        for (var j = i + 1; j < 3; j++)
        {
            var nodeI = nodes[i];
            var nodeJ = nodes[j];

            if (Math.Abs(matrix[nodeI, nodeJ] - matrix[nodeJ, nodeI]) > tolerance)
                return false;
        }
        return true;
    }

    // todo: reimplement
    public static bool ValidateMatrixHasZeroRowSums(this IStiffnessMatrix matrix, MatrixValue tolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        var nodes = matrix.Nodes;

        for (var i = 0; i < 3; i++)
        {
            var nodeI = nodes[i];

            var sum = matrix[nodeI, nodes[0]] + matrix[nodeI, nodes[1]] + matrix[nodeI, nodes[2]];
            if (Math.Abs(sum) > tolerance) return false;
        }
        return true;
    }

}