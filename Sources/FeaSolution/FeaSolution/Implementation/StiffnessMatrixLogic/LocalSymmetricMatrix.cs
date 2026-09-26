namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public class LocalSymmetricMatrix<T>(int n)
{
    private readonly T[] _data = new T[n * (n + 1) / 2];

    public T this[int i, int j]
    {
        get => _data[Index(i, j)];
        set => _data[Index(i, j)] = value;
    }

    // Transformation (i, j) -> array index
    private static int Index(int i, int j)
    {
        if (i < j) (i, j) = (j, i); // use symmetric 
        return i * (i + 1) / 2 + j;
    }
}