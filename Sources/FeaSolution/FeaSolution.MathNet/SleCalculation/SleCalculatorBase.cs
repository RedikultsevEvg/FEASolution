using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FeaSolution.MathNet.SleCalculation;

/// <summary>
/// Base class for SLE solvers that operate on <see cref="IStiffnessMatrix"/>.
/// Provides common infrastructure: dense matrix assembly, vector construction,
/// solution packing, and input validation.
/// </summary>
public abstract class SleCalculatorBase 
{
    /// <summary>
    /// Assembles a dense MathNet matrix from the sparse data of
    /// <see cref="IStiffnessMatrix"/>.
    /// Symmetrizes the result: if only the upper triangle is stored,
    /// the value is duplicated to position (j, i).
    /// </summary>
    protected static DenseMatrix BuildMathNetDenseMatrix(IStiffnessMatrix matrix)
    {
        var size = matrix.NodeCount;
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
    /// Builds a MathNet dense vector from the values of <see cref="SolutionItem"/>[].
    /// </summary>
    protected static Vector<MatrixValue> BuildMathNetVector(
        SolutionItem[] vectorFWithDirichlet)
    {
        return Vector<MatrixValue>.Build.Dense(
            vectorFWithDirichlet.Length,
            i => vectorFWithDirichlet[i].Value);
    }

    /// <summary>
    /// Packs the solution vector back into <see cref="SolutionItem"/>[],
    /// binding each value to its corresponding node.
    /// </summary>
    protected static SolutionItem[] PackSolution(
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

    /// <summary>
    /// Validates the input matrix and vector dimensions and nodes order.
    /// </summary>
    protected static void Validate(
        IStiffnessMatrix matrix,
        SolutionItem[] vectorOfMatrixSize)
    {
        int size = matrix.NodeCount;

        FeaCommonException.ThrowIfTrue(
            size == 0, "The size of source matrix is zero.");

        FeaCommonException.ThrowIfTrue(
            vectorOfMatrixSize.Length != size,
            $"Dimension mismatch: matrix size is {size}, " +
            $"vector length is {vectorOfMatrixSize.Length}.");

        var nodes = matrix.Nodes;

        for (var i = 0; i < size; i++)
        {
            FeaCommonException.ThrowIfTrue(
                !ReferenceEquals(vectorOfMatrixSize[i].Node, nodes[i]),
                $"Node order mismatch at index {i}: " +
                $"vector F has node '{vectorOfMatrixSize[i].Node.NodeId ?? "null"}', " +
                $"but matrix expects node '{nodes[i].NodeId ?? "null"}'.");
        }
    }
}