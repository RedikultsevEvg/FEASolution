using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Core.SleCalculation;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Types;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FeaSolution.MathNet.SleCalculation;

public class MathNetSleCalculator : ISleCalculator
{
    public SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet)
    {
        ArgumentNullException.ThrowIfNull(matrixKWithDirichlet);
        ArgumentNullException.ThrowIfNull(vectorFWithDirichlet);

        // todo Проверить что порядок узлов совпадает.
        Validate(matrixKWithDirichlet, vectorFWithDirichlet);

        var invertedMatrixK = GetInvertedMatrix(matrixKWithDirichlet);

        var vectorF = Vector<MatrixValue>.Build.Dense(
            vectorFWithDirichlet.Length,
            i => vectorFWithDirichlet[i].Value);   

        var solutionVectorT = Multiplay(invertedMatrixK, vectorF);

        // Преобразуем Vector<MatrixValue> обратно в SolutionItem[].
        // Индекс i в solutionVectorT соответствует узлу matrix.Nodes[i].
        var nodes = matrixKWithDirichlet.Nodes;

        FeaCommonException.ThrowIfTrue(
            solutionVectorT.Count != nodes.Count,
            $"Solution vector length ({solutionVectorT.Count}) " +
            $"doesn't match node count ({nodes.Count}).");

        var result = new SolutionItem[nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            var vectorTValue = solutionVectorT[i];

            FeaCommonException.ThrowIfTrue(MatrixValue.IsNaN(vectorTValue), 
                "At least one result value is NaN. The solution can't be calculated.");

            result[i] = new SolutionItem
            {
                Node = nodes[i],
                Freedom = Freedom.Temperature,
                Value = vectorTValue
            };
        }

        return result;
    }

    internal Vector<MatrixValue> Multiplay(Matrix<MatrixValue> matrix, Vector<MatrixValue> vector)
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

    internal Matrix<MatrixValue> GetInvertedMatrix(IStiffnessMatrix matrix)
    {
        int size = matrix.NodeCount;

        // 1. Собираем плотную матрицу MathNet из разреженных данных.
        //    Обратная к разреженной матрице почти всегда плотная,
        //    поэтому DenseMatrix здесь уместнее SparseMatrix.
        var dense = DenseMatrix.Create(size, size, 0.0);

        foreach (var (i, j, value) in matrix.NonZeroMatrixValuesWithIndexes())
        {
            dense[i, j] = value;

            // Исходная матрица симметричная — на всякий случай дублируем,
            // если в хранилище лежит только одна половина.
            if (i != j)
                dense[j, i] = value;
        }

        // 2. Считаем обратную матрицу средствами MathNet.
        return InverseMathnetDenseMatrix(dense);
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

    private void Validate(IStiffnessMatrix matrix, SolutionItem[] vectorOfMatrixSize)
    {
        int size = matrix.NodeCount;

        FeaCommonException.ThrowIfTrue(size == 0, "The size of source matrix is zero.");

        FeaCommonException.ThrowIfTrue(
            vectorOfMatrixSize.Length != size,
            $"Dimension mismatch: matrix size is {size}, " +
            $"vector length is {vectorOfMatrixSize.Length}.");
    }
}