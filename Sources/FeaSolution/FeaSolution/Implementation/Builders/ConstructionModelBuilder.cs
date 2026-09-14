using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.Implementation.Builders;

public class ConstructionModelBuilder
{
    /// <summary>
    /// Creates construction model.
    /// </summary>
    /// <returns>A new construction model.</returns>
    public ConstructionModel CreateModel()
        => new()
        {
            AllowedNodeType = NodesType,
        };

    /// <summary>
    /// Sets the node type for all finite elements in construction model.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <returns>The reference to the current builder.</returns>
    public  ConstructionModelBuilder SetNodesType(ElementNodeType nodeType)
    {
        NodesType = nodeType;
        return this;
    }

    private ElementNodeType NodesType { get; set; } = new();
}