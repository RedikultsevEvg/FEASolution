using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.Implementation.Builders;

public static class ConstructionModelBuilder
{
    /// <summary>
    /// Creates a new structural model.
    /// </summary>
    /// <returns>A new structural model with a 1D node type.</returns>
    public static ConstructionModel CreateNew1D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew1D(),
        };

    /// <summary>
    /// Creates a new structural model.
    /// </summary>
    /// <returns>A new structural model with a 2D node type.</returns>
    public static ConstructionModel CreateNew2D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew2D(),
        };

    /// <summary>
    /// Creates a new structural model.
    /// </summary>
    /// <returns>A new structural model with a 3D node type.</returns>
    public static ConstructionModel CreateNew3D()
        => new()
        {
            AllowedNodeType = ElementNodeTypeBuilder.CreateNew3D(),
        };
}