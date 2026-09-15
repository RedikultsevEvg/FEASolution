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
    {
        if (NodeType == null)
        {
            throw new FeaModelBuilderException(NodeTypeNullExceptionMessage);
        }

        return new ConstructionModel
        {
            AllowedNodeType = NodeType,
            Elements = Elements
        };
    }

    /// <summary>
    /// Sets the node type for all finite elements in construction model.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <returns>The reference to the current builder.</returns>
    public  ConstructionModelBuilder SetNodesType(ElementNodeType nodeType)
    {
        ArgumentNullException.ThrowIfNull(nodeType);
        if (Elements.Count != 0)
        {
            throw new FeaModelBuilderException("You have to remove all elements before changing node type.");
        }

        NodeType = nodeType;
        return this;
    }

    public ConstructionModelBuilder AddElements(params ICollection<IFiniteElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (NodeType == null!)
        {
            throw new FeaModelBuilderException(NodeTypeNullExceptionMessage);
        }

        if (elements.Any(element => element.ElementType.NodeType.Dimension != NodeType.Dimension))
        {
            throw new FeaModelBuilderException($"Dimensional of all elements should be equal to {NodeType.Dimension.ToString()}.");
        }

        foreach (var element in elements)
        {
            Elements.Add(element);
        }
        return this;

    }

    private ElementNodeType? NodeType { get; set; }

    private ICollection<IFiniteElement> Elements { get; } = [];

    private const string NodeTypeNullExceptionMessage = $"Use method '{nameof(SetNodesType)}' to set the model node type.";
}