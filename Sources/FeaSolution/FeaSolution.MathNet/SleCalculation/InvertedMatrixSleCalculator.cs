using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FeaSolution.MathNet.SleCalculation;

/// <summary>
/// SLE calculator by method of inverted matrix.
/// </summary>
public class InvertedMatrixSleCalculator : SleCalculatorBase, ISleCalculator
{
    /// <inheritdoc/>
    public SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet)
    {
        ArgumentNullException.ThrowIfNull(matrixKWithDirichlet);
        ArgumentNullException.ThrowIfNull(vectorFWithDirichlet);

        Validate(matrixKWithDirichlet, vectorFWithDirichlet);

        var mathNetDenseMatrix = BuildMathNetDenseMatrix(matrixKWithDirichlet);

        var invertedMatrixK = InverseMathnetDenseMatrix(mathNetDenseMatrix); 

        var vectorF = Vector<MatrixValue>.Build.Dense(
            vectorFWithDirichlet.Length,
            i => vectorFWithDirichlet[i].Value);   

        var solutionVectorT = Multiplay(invertedMatrixK, vectorF);

        return PackSolution(matrixKWithDirichlet.Nodes, solutionVectorT);
    }

    private static Vector<MatrixValue> Multiplay(Matrix<MatrixValue> matrix, Vector<MatrixValue> vector)
    {
        FeaCommonException.ThrowIfTrue(
            matrix == null!, "The inverted matrix is null.");

        FeaCommonException.ThrowIfTrue(
            vector == null!, "The vector F is null.");

        int rows = matrix!.RowCount;
        int cols = matrix.ColumnCount;

        FeaCommonException.ThrowIfTrue(
            rows != cols,
            $"The matrix must be square. Actual size: {rows}x{cols}.");

        FeaCommonException.ThrowIfTrue(
            vector!.Count != cols,
            $"Dimension mismatch: matrix is {rows}x{cols}, " +
            $"vector length is {vector.Count}.");

        // 2. Умножаем матрицу на вектор средствами MathNet.
        return matrix.Multiply(vector);
    }

    internal static Matrix<MatrixValue> InverseMathnetDenseMatrix(DenseMatrix dense)
    {
        Matrix<MatrixValue> inverted;
        try
        {
            inverted = dense.Inverse();
        }
        catch (Exception ex)
        {
            throw new FeaCommonException("Can't calculate the inverted matrix.", ex);
        }

        return inverted;
    }
}