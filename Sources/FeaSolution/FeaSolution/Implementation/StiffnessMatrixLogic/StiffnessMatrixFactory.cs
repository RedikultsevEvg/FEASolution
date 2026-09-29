using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

internal static class StiffnessMatrixFactory
{
    internal static IStiffnessMatrix CreateNew(IReadOnlyList<IElementNode>? nodes = null)
    {
        if (nodes != null && nodes.Count <= 4)
        {
            return new SmallStiffnessMatrix(nodes);
        }

        return new StiffnessMatrix();
    }
}