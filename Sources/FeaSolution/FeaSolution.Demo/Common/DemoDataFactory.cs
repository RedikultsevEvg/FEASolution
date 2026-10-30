namespace FeaSolution.Demo.Common;

/// <summary>
/// Фабрика треугольной сетки. Возвращает описание:
/// идентификаторы элементов и координаты узлов.
/// </summary>
internal static class DemoDataFactory
{
    public static TriangularCoordinatesProvider CreateTrianglesCoordinates(int elementCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elementCount, 1);

        // Нижний ряд: b0..b_N, X = i, Y = 0. Верхний ряд: t0..t_N, X = i, Y = 1.
        var bottom = new (double X, double Y)[elementCount + 1];
        for (var i = 0; i < bottom.Length; i++)
            bottom[i] = (i, 0);

        var top = new (double X, double Y)[elementCount + 1];
        for (var i = 0; i < top.Length; i++)
            top[i] = (i, 1);

        var triangles = new List<TriangleDescriptor>(elementCount);
        for (var i = 0; i < elementCount; i++)
        {
            var k = i / 2;
            var (node1, node2, node3) = (i % 2 == 0)
                ? (bottom[k], bottom[k + 1], top[k])   // чётный: (b[k], b[k+1], t[k])
                : (top[k], top[k + 1], bottom[k + 1]);  // нечётный: (t[k], t[k+1], b[k+1])

            triangles.Add(new TriangleDescriptor($"e{i}", node1, node2, node3));
        }

        return new TriangularCoordinatesProvider { Triangles = triangles };
    }
}

/// <summary>
/// Описание одного треугольного элемента: идентификатор и координаты трёх узлов.
/// </summary>
internal sealed record TriangleDescriptor(
    string ElementId,
    (double X, double Y) Node1,
    (double X, double Y) Node2,
    (double X, double Y) Node3);

/// <summary>
/// Описание треугольной сетки: коллекция описаний элементов.
/// </summary>
internal sealed class TriangularCoordinatesProvider
{
    public required IReadOnlyList<TriangleDescriptor> Triangles { get; init; }
}