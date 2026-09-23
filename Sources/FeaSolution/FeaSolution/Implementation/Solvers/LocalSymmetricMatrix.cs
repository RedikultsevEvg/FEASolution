namespace FeaSolution.Implementation.Solvers;

public class LocalSymmetricMatrix<T>(int n)
{
    private readonly T[] _data = new T[n * (n + 1) / 2];
    private readonly int _n = n;

    // Количество элементов в треугольнике: n*(n+1)/2

    // Преобразование (i, j) -> индекс в массиве (для нижнего треугольника)
    private static int Index(int i, int j)
    {
        if (i < j) (i, j) = (j, i); // используем симметрию
        return i * (i + 1) / 2 + j;
    }

    public T this[int i, int j]
    {
        get => _data[Index(i, j)];
        set => _data[Index(i, j)] = value;
    }
}