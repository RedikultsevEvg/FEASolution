using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModels;

namespace FeaSolution.Implementation.Builders;

/// <summary>
/// Construction model builder.
/// </summary>
public class ConstructionModelBuilder
{
    private const CoordinateValue DeltaDefaultValue = 0.0001;

    private ElementNodeType? NodeType { get; set; }

    private ICollection<IFiniteElement> Elements { get; } = [];

    private const string NodeTypeNullExceptionMessage 
        = $"Use method '{nameof(SetNodesType)}' to set the model node type.";

    /// <summary>
    /// Creates construction model.
    /// </summary>
    /// <returns>A new construction model.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public ConstructionModel Build()
    {
        FeaModelBuilderException.ThrowIfTrue(
            !Elements.Any(), 
            $"Can't build the model with empty element collection. Use '{nameof(AddElements)}' to add elements.");

        FeaModelBuilderException.ThrowIfTrue(NodeType == null, NodeTypeNullExceptionMessage);

        var freedomsIntersection = Elements
            .Select(e => e.Type
                .Freedoms
                .Select(f => f.Item1)
                .ToHashSet())
            .Aggregate((current, next) =>
            {
                current.IntersectWith(next); 
                return current;
            })
            .ToArray();

        FeaModelBuilderException.ThrowIfTrue(
            !freedomsIntersection.Any(),
            "Can't build the model - all added elements should have at least one common freedom.");

        return new ConstructionModel
        {
            AllowedNodeType = NodeType!,
            Elements = Elements,
            CommonFreedoms = freedomsIntersection
        };
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

        FeaModelBuilderException.ThrowIfTrue(
            elements.Count == 0,
            "Can't add an empty element collection. Pass at least one element.");

        FeaModelBuilderException.ThrowIfTrue(elements.Any(e => e == null!),
            "Element collection contains null elements.");

        var distinctDimensions = elements
            .Select(element => element.Type.NodeType.Dimension)
            .Distinct()
            .ToArray();

        FeaModelBuilderException.ThrowIfTrue(
            distinctDimensions.Length > 1,
            $"All added elements should have the same dimension. " +
            $"Found dimensions: {string.Join(", ", distinctDimensions)}.");

        var dimension = distinctDimensions[0];

        FeaModelBuilderException.ThrowIfTrue(NodeType != null && NodeType.Dimension != dimension,
            $"Dimension of added elements ({dimension}) should be equal to the already configured node type dimension ({NodeType?.Dimension}).");

        if (NodeType == null)
        {
            SetNodesType(elements.First().Type.NodeType);
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
    /// <param name="elementsToMerge">Elements to merge. The null-value accepted and means to merge nodes in all model elements.</param>
    /// <param name="delta">Maximum difference in coordinates for common node.</param>
    /// <returns></returns>
    public ConstructionModelBuilder Merge(IEnumerable<IFiniteElement>? elementsToMerge = null, CoordinateValue delta = DeltaDefaultValue)
    {
        if (elementsToMerge == null)
        {
            // merge all elements in model.
        }

        return this;
    }

    private ConstructionModelBuilder SetNodesType(ElementNodeType nodeType)
    {
        ArgumentNullException.ThrowIfNull(nodeType);
        FeaModelBuilderException.ThrowIfTrue(
            Elements.Count != 0,
            "You have to remove all elements before changing node type.");

        NodeType = nodeType;
        return this;
    }
}