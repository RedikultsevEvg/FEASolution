namespace FeaSolution.Implementation.StiffnessMatrixLogic;

internal static class StiffnessMatrixFactory
{
    private const int Treasure = 1600;

    internal static IStiffnessMatrix CreateNew(int elementCount = 0)
    {
        if (elementCount < Treasure)
        {
            return new SmallStiffnessMatrix();
        }

        return new StiffnessMatrix();
    }
}