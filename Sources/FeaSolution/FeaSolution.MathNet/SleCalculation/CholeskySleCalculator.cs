using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FeaSolution.MathNet.SleCalculation;

/// <summary>
/// Решатель СЛАУ на основе разложения Холецкого.
/// Требует, чтобы матрица K после учёта условий Дирихле была
/// симметричной положительно определённой (SPD) — что типично
/// для задач теплопроводности и упругости в МКЭ.
/// </summary>
public class CholeskySleCalculator : ISleCalculator
{
    public SolutionItem[] SolveSLE(
        IStiffnessMatrix matrixKWithDirichlet,
        SolutionItem[] vectorFWithDirichlet)
    {
        ArgumentNullException.ThrowIfNull(matrixKWithDirichlet);
        ArgumentNullException.ThrowIfNull(vectorFWithDirichlet);

        // todo Проверить что порядок узлов совпадает.
        Validate(matrixKWithDirichlet, vectorFWithDirichlet);

        // 1. Собираем плотную матрицу K.
        var denseK = BuildDenseMatrix(matrixKWithDirichlet);

        // 2. Собираем вектор F.
        var vectorF = Vector<MatrixValue>.Build.Dense(
            vectorFWithDirichlet.Length,
            i => vectorFWithDirichlet[i].Value);

        // 3. Решаем K·T = F через разложение Холецкого.
        var solutionVectorT = SolveWithCholesky(denseK, vectorF);

        // 4. Упаковываем решение обратно в SolutionItem[].
        return PackSolution(matrixKWithDirichlet.Nodes, solutionVectorT);
    }

    /// <summary>
    /// Решает систему K·T = F через разложение Холецкого.
    /// K должна быть симметричной положительно определённой.
    /// </summary>
    internal static Vector<MatrixValue> SolveWithCholesky(
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

    /// <summary>
    /// Собирает плотную матрицу MathNet из разреженных данных IStiffnessMatrix.
    /// Симметризует: если в хранилище лежит только верхний треугольник,
    /// дублирует значение на позицию (j, i).
    /// </summary>
    internal static DenseMatrix BuildDenseMatrix(IStiffnessMatrix matrix)
    {
        int size = matrix.NodeCount;
        var dense = DenseMatrix.Create(size, size, 0.0);

        foreach (var (i, j, value) in matrix.NonZeroMatrixValuesWithIndexes())
        {
            dense[i, j] = value;

            if (i != j)
                dense[j, i] = value;
        }

        return dense;
    }

    /// <summary>
    /// Упаковывает вектор решения в массив SolutionItem,
    /// привязывая каждое значение к соответствующему узлу.
    /// </summary>
    internal static SolutionItem[] PackSolution(
        IReadOnlyList<IElementNode> nodes,
        Vector<MatrixValue> solutionVectorT)
    {
        FeaCommonException.ThrowIfTrue(
            solutionVectorT.Count != nodes.Count,
            $"Solution vector length ({solutionVectorT.Count}) " +
            $"doesn't match node count ({nodes.Count}).");

        var result = new SolutionItem[nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            var value = solutionVectorT[i];

            FeaCommonException.ThrowIfTrue(
                MatrixValue.IsNaN(value),
                "At least one result value is NaN. " +
                "The solution can't be calculated.");

            result[i] = new SolutionItem
            {
                Node = nodes[i],
                Freedom = Freedom.Temperature,
                Value = value
            };
        }

        return result;
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