using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FeaSolution.MathNet.SleCalculation;

/// <summary>
/// SLE solver based on Cholesky decomposition.
/// Requires that the matrix K, after applying Dirichlet conditions,
/// be symmetric positive definite (SPD) — which is typical
/// for heat conduction and elasticity problems in FEM.
/// </summary>
public class CholeskySleCalculator : SleCalculatorBase, ISleCalculator
{
    /// <inheritdoc/>
    public SolutionItem[] SolveSLE(
        IStiffnessMatrix matrixKWithDirichlet,
        SolutionItem[] vectorFWithDirichlet)
    {
        ArgumentNullException.ThrowIfNull(matrixKWithDirichlet);
        ArgumentNullException.ThrowIfNull(vectorFWithDirichlet);

        Validate(matrixKWithDirichlet, vectorFWithDirichlet);

        var mathNetDenseMatrix = BuildMathNetDenseMatrix(matrixKWithDirichlet);

        var mathNetVectorF = BuildMathNetVector(vectorFWithDirichlet);

        var solutionVectorT = SolveWithCholesky(mathNetDenseMatrix, mathNetVectorF);

        return PackSolution(matrixKWithDirichlet.Nodes, solutionVectorT);
    }

    private static Vector<MatrixValue> SolveWithCholesky(
        DenseMatrix denseK,
        Vector<MatrixValue> vectorF)
    {
        FeaCommonException.ThrowIfTrue(
            denseK == null!, "The stiffness matrix is null.");

        FeaCommonException.ThrowIfTrue(
            vectorF == null!, "The vector F is null.");

        var rows = denseK!.RowCount;
        var cols = denseK.ColumnCount;

        FeaCommonException.ThrowIfTrue(
            rows != cols,
            $"The matrix must be square. Actual size: {rows}x{cols}.");

        FeaCommonException.ThrowIfTrue(
            vectorF!.Count != cols,
            $"Dimension mismatch: matrix is {rows}x{cols}, " +
            $"vector length is {vectorF.Count}.");

        Vector<MatrixValue> solution;
        try
        {
            var cholesky = denseK.Cholesky();
            solution = cholesky.Solve(vectorF);
        }
        catch (Exception ex)
        {
            throw new FeaCommonException(
                "Can't solve the SLE: the matrix is not symmetric " +
                "positive definite or is ill-conditioned.",
                ex);
        }

        return solution;
    }
}