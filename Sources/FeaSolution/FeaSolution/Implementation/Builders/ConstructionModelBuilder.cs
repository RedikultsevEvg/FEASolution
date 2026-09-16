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
    public ConstructionModel Build()
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

    /// <summary>
    /// Adds elements to the model.
    /// </summary>
    /// <param name="elements">Collection of elements.</param>
    /// <returns></returns>
    /// <exception cref="FeaModelBuilderException"></exception>
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

    /// <summary>
    /// Find and union the common nodes.
    /// </summary>
    /// <param name="delta"></param>
    /// <returns></returns>
    public ConstructionModelBuilder Merge(CoordinateValue delta = DeltaDefaultValue)
    {
        return this;
    }

    private const CoordinateValue DeltaDefaultValue = 0.0001;

    private ElementNodeType? NodeType { get; set; }

    private ICollection<IFiniteElement> Elements { get; } = [];

    private const string NodeTypeNullExceptionMessage = $"Use method '{nameof(SetNodesType)}' to set the model node type.";

}