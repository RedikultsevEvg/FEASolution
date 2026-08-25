using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.Implementation.Builders;

public static class ConstructionModelBuilder
{
    /// <summary>
    /// Создает новую модель конструкции.
    /// </summary>
    /// <returns>Новая модель конструкции с типом узла 1D.</returns>
    public static ConstructionModel CreateNew1D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew1D(),
        };

    /// <summary>
    /// Создает новую модель конструкции.
    /// </summary>
    /// <returns>Новая модель конструкции с типом узла 2D.</returns>
    public static ConstructionModel CreateNew2D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew2D(),
        };

    /// <summary>
    /// Создает новую модель конструкции.
    /// </summary>
    /// <returns>Новая модель конструкции с типом узла 3D.</returns>
    public static ConstructionModel CreateNew3D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew3D(),
        };
}