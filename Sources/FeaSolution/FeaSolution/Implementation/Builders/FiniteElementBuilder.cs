using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ElementNodes;
using FeaSolution.Implementation.FiniteElements;

namespace FeaSolution.Implementation.Builders;

// todo: необходим нагрузочный тест на билдер!
public class FiniteElementBuilder
{
    private ElementNodeType NodeType { get; set; } = new();

    private ICollection<IElementNode> Nodes { get; } = [];

    private List<(Freedom, IElementOptions)> Freedoms { get; } = [];

    /// <summary>
    /// Creates a new finite element.
    /// </summary>
    /// <returns>The new element.</returns>
    public IFiniteElement Build(string userId = "")
    {
        ArgumentNullException.ThrowIfNull(NodeType);
        ArgumentNullException.ThrowIfNull(Nodes);
        FeaElementBuilderException.ThrowIfTrue(Nodes.Count == 0, $"Can't build element with empty node collection. Use '{nameof(AddNode)}' to add nodes.");
        FeaElementBuilderException.ThrowIfTrue(Freedoms.Count == 0, $"Can't build element with empty freedom collection. Use '{nameof(AddFreedom)}' to add freedoms.");

        var elementTypeFreedoms = new List<(Freedom, IElementOptions)>();
        elementTypeFreedoms.AddRange(Freedoms);

        var elementType = new FiniteElementType
        {
            NodeType = NodeType,
            Freedoms = elementTypeFreedoms,
        };

        var newElement = new FiniteElement
        {
            UserId = userId,
            Type = elementType,
            Nodes = Nodes,
        };

        return newElement;
    }

    /// <summary>
    /// Sets the node type for elements.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <returns>The reference to the current builder.</returns>
    public FiniteElementBuilder SetNodesType(ElementNodeType nodeType)
    {
        FeaElementBuilderException.ThrowIfTrue(Nodes.Count != 0, "You have to remove all nodes before changing node type.");

        NodeType = nodeType;
        return this;
    }

    /// <summary>
    /// Adds 1D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 1D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x)
    {
        if (NodeType.Dimension != Dimensional.OneDimensional)
        {
            throw new FeaElementBuilderException(NodeType);
        }

        var node = new ElementNode(NodeType)
        {
            X = x,
        };

        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds 2D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="y">The Y-coordinate.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 2D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y)
    {
        if (NodeType.Dimension != Dimensional.TwoDimensional)
        {
            throw new FeaElementBuilderException(NodeType);
        }

        var node = new ElementNode(NodeType)
        {
            X = x,
            Y = y,
        };

        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds 3D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="y">The Y-coordinate.</param>
    /// <param name="z">The Z-coordinate.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 3D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y, CoordinateValue z)
    {
        if (NodeType.Dimension != Dimensional.ThreeDimensional)
        {
            throw new FeaElementBuilderException(NodeType);
        }

        var node = new ElementNode(NodeType)
        {
            X = x,
            Y = y,
            Z = z
        };

        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds freedom and options to element.
    /// </summary>
    /// <param name="freedom"></param>
    /// <param name="options"></param>
    /// <returns>The reference to the current builder.</returns>
    public FiniteElementBuilder AddFreedom(Freedom freedom, IElementOptions options)
    {
        FeaElementBuilderException.ThrowIfTrue(options == null!, "Can't add null options.");
        Freedoms.Add((freedom,options)!);
        return this;
    }
}