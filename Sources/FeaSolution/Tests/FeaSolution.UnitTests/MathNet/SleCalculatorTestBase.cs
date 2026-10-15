using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;
using NSubstitute;

namespace FeaSolution.UnitTests.MathNet;

/// <summary>
/// Общие хелперы для тестов MathNetSleCalculator.
/// </summary>
public abstract class SleCalculatorTestBase
{
    protected static IElementNode MakeNodeMock(string userId)
    {
        var node = Substitute.For<IElementNode>();
        node.UserId.Returns(userId);
        return node;
    }

    protected static IElementNode[] MakeNodes(int count, string prefix = "node")
    {
        var nodes = new IElementNode[count];
        for (int i = 0; i < count; i++)
            nodes[i] = MakeNodeMock($"{prefix}{i + 1}");
        return nodes;
    }

    protected static SolutionItem MakeItem(IElementNode node, double value)
        => new()
        {
            Node = node,
            Freedom = Freedom.Temperature,
            Value = value
        };

    protected static SolutionItem[] MakeVector(
        IReadOnlyList<IElementNode> nodes,
        params double[] values)
    {
        if (nodes.Count != values.Length)
            throw new ArgumentException(
                $"Nodes count ({nodes.Count}) != values length ({values.Length})");

        var result = new SolutionItem[values.Length];
        for (int i = 0; i < values.Length; i++)
            result[i] = MakeItem(nodes[i], values[i]);
        return result;
    }

    // ===============================================================
    // Мок матрицы жёсткости
    // ===============================================================

    protected static IStiffnessMatrix MakeMatrixMock(
        int size,
        IReadOnlyList<IElementNode> nodes,
        IEnumerable<(int indexI, int indexJ, double value)> values)
    {
        var matrix = Substitute.For<IStiffnessMatrix>();

        matrix.NodeCount.Returns(size);
        matrix.Nodes.Returns(nodes);
        matrix.NonZeroMatrixValuesWithIndexes().Returns(values);

        return matrix;
    }

    // ===============================================================
    // Генераторы разреженных матриц
    // ===============================================================

    /// <summary>
    /// Трёхдиагональная симметричная матрица N×N:
    ///   K[i,i]   =  2
    ///   K[i,i+1] = -1
    /// Возвращает только верхний треугольник — как хранит StiffnessMatrix.
    /// </summary>
    protected static (int i, int j, double v)[] TridiagonalMatrix(int size)
    {
        var values = new List<(int, int, double)>(2 * size - 1);

        for (int i = 0; i < size; i++)
            values.Add((i, i, 2.0));

        for (int i = 0; i < size - 1; i++)
            values.Add((i, i + 1, -1.0));

        return values.ToArray();
    }

    /// <summary>
    /// Диагональная матрица N×N с заданными диагональными элементами.
    /// </summary>
    protected static (int i, int j, double v)[] DiagonalMatrix(params double[] diagonal)
    {
        var values = new List<(int, int, double)>(diagonal.Length);

        for (int i = 0; i < diagonal.Length; i++)
            values.Add((i, i, diagonal[i]));

        return values.ToArray();
    }

    // ===============================================================
    // Утилиты для вычислений
    // ===============================================================

    /// <summary>
    /// Собирает плотную симметричную матрицу size×size из значений
    /// и умножает её на вектор t. Возвращает F = K · T.
    /// Используется для получения эталонной правой части без ручного счёта.
    /// </summary>
    protected static double[] MultiplyDense(
        IEnumerable<(int i, int j, double v)> values,
        double[] t,
        int size)
    {
        if (t.Length != size)
            throw new ArgumentException(
                $"Vector length ({t.Length}) != matrix size ({size})");

        var dense = new double[size, size];
        foreach (var (i, j, v) in values)
        {
            dense[i, j] = v;
            if (i != j) dense[j, i] = v;
        }

        var f = new double[size];
        for (int i = 0; i < size; i++)
        {
            double sum = 0;
            for (int j = 0; j < size; j++)
                sum += dense[i, j] * t[j];
            f[i] = sum;
        }
        return f;
    }
}