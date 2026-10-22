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
    private ElementNodeType? NodeType { get; set; } 

    private ICollection<IElementNode> Nodes { get; } = [];

    private List<(Freedom, IElementOptions)> Freedoms { get; } = [];

    /// <summary>
    /// Creates a new finite element.
    /// </summary>
    /// <returns>The new element.</returns>
    public IFiniteElement Build(string userId = "")
    {
        ArgumentNullException.ThrowIfNull(Nodes);
        FeaElementBuilderException.ThrowIfTrue(Nodes.Count == 0, $"Can't build element with empty node collection. Use '{nameof(AddNode)}' to add nodes.");
        FeaElementBuilderException.ThrowIfTrue(Freedoms.Count == 0, $"Can't build element with empty freedom collection. Use '{nameof(AddFreedom)}' to add freedoms.");
        ArgumentNullException.ThrowIfNull(NodeType);

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
    /// Adds 1D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="nodeId">Node ID.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 1D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, string nodeId = "")
    {
        ValidateNodesType(ElementNodeType.Type1D);

        var node = new ElementNode(NodeType!)
        {
            X = x,
            NodeId = nodeId
        };

        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds 2D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="y">The Y-coordinate.</param>
    /// <param name="nodeId">Node ID.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 2D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y, string nodeId = "")
    {
        ValidateNodesType(ElementNodeType.Type2D);

        var node = new ElementNode(NodeType!)
        {
            X = x,
            Y = y,
            NodeId = nodeId
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
    /// <param name="nodeId">Node ID.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaCommonException">Raise an exception if builder dimension is not 3D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y, CoordinateValue z, string nodeId = "")
    {
        ValidateNodesType(ElementNodeType.Type3D);

        var node = new ElementNode(NodeType!)
        {
            X = x,
            Y = y,
            Z = z,
            NodeId = nodeId
        };

        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds existing node.
    /// </summary>
    /// <param name="node">Existing node.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaElementBuilderException">Raise an exception if builder dimension is not 3D.</exception>
    public FiniteElementBuilder AddNode(IElementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        ValidateNodesType(node.Type);
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

    private void ValidateNodesType(ElementNodeType nodeType)
    {
        NodeType ??= nodeType;

        FeaElementBuilderException.ThrowIfTrue(NodeType.Dimension != nodeType.Dimension,
            $"Node has dimension not equal to '{NodeType.Dimension.ToString()}'");
    }
}