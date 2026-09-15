using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
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
            AllowedNodeType = NodeType,
            Elements = Elements
        };

    /// <summary>
    /// Sets the node type for all finite elements in construction model.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <returns>The reference to the current builder.</returns>
    public  ConstructionModelBuilder SetNodesType(ElementNodeType nodeType)
    {
        NodeType = nodeType;
        return this;
    }

    public ConstructionModelBuilder AddElements(params ICollection<IFiniteElement> elements)
    {
        if (elements.Any(e => e.ElementType.NodeType != NodeType))
        {
            throw new FeaCommonException($"All added elements should have node type = {NodeType.Dimension.ToString()}");
        }

        foreach (var element in elements)
        {
            Elements.Add(element);
        }
        return this;

    }

    private ElementNodeType NodeType { get; set; } = new();

    private ICollection<IFiniteElement> Elements { get; set; } = [];
}