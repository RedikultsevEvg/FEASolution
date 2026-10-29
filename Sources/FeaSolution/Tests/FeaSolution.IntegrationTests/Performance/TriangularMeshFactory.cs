namespace FeaSolution.IntegrationTests.Performance;

/// <summary>
/// Фабрика треугольной сетки. Возвращает ТОЛЬКО описание:
/// идентификаторы элементов и координаты узлов.
/// Создание узлов/элементов/модели — ответственность вызывающего кода
/// (и попадает в замер в тестах).
/// </summary>
internal static class TriangularMeshFactory
{
    public static TriangularMeshDescriptor Create(int elementCount)
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
            var (n1, n2, n3) = (i % 2 == 0)
                ? (bottom[k], bottom[k + 1], top[k])   // чётный: (b[k], b[k+1], t[k])
                : (top[k], top[k + 1], bottom[k + 1]);  // нечётный: (t[k], t[k+1], b[k+1])

            triangles.Add(new TriangleDescriptor($"e{i}", n1, n2, n3));
        }

        return new TriangularMeshDescriptor { Triangles = triangles };
    }
}

/// <summary>
/// Описание одного треугольного элемента: идентификатор и координаты трёх узлов.
/// Узлы НЕ создаются — только координаты. Общие узлы между элементами
/// определяются по совпадению координат.
/// </summary>
internal sealed record TriangleDescriptor(
    string ElementId,
    (double X, double Y) N1,
    (double X, double Y) N2,
    (double X, double Y) N3);

/// <summary>
/// Описание треугольной сетки: только идентификаторы элементов и координаты узлов.
/// Никаких IElementNode/IFiniteElement/IConstructionModel — всё это создаётся в тесте.
/// </summary>
internal sealed class TriangularMeshDescriptor
{
    public required IReadOnlyList<TriangleDescriptor> Triangles { get; init; }
}