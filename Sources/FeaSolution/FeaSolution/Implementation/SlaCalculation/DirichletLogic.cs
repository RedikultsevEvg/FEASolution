using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.SlaCalculation;

/// <summary>
/// Dirichlet logic.
/// </summary>
internal class DirichletLogic : IDirichletLogic
{
    /// <inheritdoc/>
    public void ApplyDirichlet(IStiffnessMatrix matrixK,
        SolutionItem[] vectorF,
        ICollection<DirichleCondition> dirichleConditions)
    {
        ArgumentNullException.ThrowIfNull(vectorF);
        ArgumentNullException.ThrowIfNull(matrixK);
        ArgumentNullException.ThrowIfNull(dirichleConditions);
        FeaCommonException.ThrowIfTrue(dirichleConditions.Count == 0, "The collection of Dirichle condition is empty.");

        foreach (var condition in dirichleConditions)
        {
            ApplyDirichletOnce(matrixK, vectorF, condition);
        }
    }

    internal void ApplyDirichletOnce(IStiffnessMatrix matrixK,
           SolutionItem[] vectorF, DirichleCondition condition)
    {
        ArgumentNullException.ThrowIfNull(matrixK);
        ArgumentNullException.ThrowIfNull(vectorF);
        ArgumentNullException.ThrowIfNull(condition);

        var nodes = matrixK.Nodes;
        var constrainedNode = condition.Node;
        var prescribedValue = condition.Value;

        // Находим индекс заданного узла в матрице.
        // В StiffnessMatrix порядок Nodes совпадает с порядком добавления,
        // и vectorF ожидается в том же порядке.
        var nodeIndex = FindNodeIndex(matrixK, constrainedNode);
        FeaCommonException.ThrowIfTrue(nodeIndex < 0, "The constrained node was not found in the stiffness matrix.");
        FeaCommonException.ThrowIfTrue(nodeIndex >= vectorF.Length, "The vector F is smaller than the matrix size.");

        // -----------------------------------------------------------------
        // Шаг 1. F[j] -= K[j,i] * T для всех j != i
        // Это делается ДО зануления столбца, иначе потеряем вклад.
        // -----------------------------------------------------------------
        for (var j = 0; j < nodes.Count; j++)
        {
            if (j == nodeIndex) continue;

            var kji = matrixK[nodes[j], constrainedNode];
            if (kji == 0.0) continue;

            vectorF[j].Value -= kji * prescribedValue;
        }

        // -----------------------------------------------------------------
        // Шаг 2. Занулить столбец i: K[j,i] = 0 для j != i
        // -----------------------------------------------------------------
        for (var j = 0; j < nodes.Count; j++)
        {
            if (j == nodeIndex) continue;

            matrixK[nodes[j], constrainedNode] = 0.0;
        }

        // -----------------------------------------------------------------
        // Шаг 3. Занулить строку i: K[i,j] = 0 для j != i
        // -----------------------------------------------------------------
        for (var j = 0; j < nodes.Count; j++)
        {
            if (j == nodeIndex) continue;

            matrixK[constrainedNode, nodes[j]] = 0.0;
        }

        // -----------------------------------------------------------------
        // Шаг 4. K[i,i] = 1
        // -----------------------------------------------------------------
        matrixK[constrainedNode, constrainedNode] = 1.0;

        // -----------------------------------------------------------------
        // Шаг 5. F[i] = T
        // -----------------------------------------------------------------
        vectorF[nodeIndex].Value = prescribedValue;
    }

    /// <summary>
    /// Ищет индекс узла в матрице по ссылке.
    /// </summary>
    private static int FindNodeIndex(IStiffnessMatrix matrix, IElementNode node)
    {
        var nodes = matrix.Nodes;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (ReferenceEquals(nodes[i], node))
                return i;
        }
        return -1;
    }
}