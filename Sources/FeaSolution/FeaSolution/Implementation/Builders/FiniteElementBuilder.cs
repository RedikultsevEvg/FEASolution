using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ElementNodes;
using FeaSolution.Implementation.FiniteElements;

namespace FeaSolution.Implementation.Builders;

public class FiniteElementBuilder
{
    /// <summary>
    /// Creates a new finite element.
    /// </summary>
    /// <returns>The new element.</returns>
    public IFiniteElement CreateElement(string userId = "")
    {
        var elementType = new FiniteElementType
        {
            NodeType = NodeType,
            Freedoms =
            [
                new DegreeOfFreedom()
            ],
            StiffnessMatrixCalculationMethod = null!
        };

        var newElement = new FiniteElement(userId, elementType);

        return newElement;
    }

    /// <summary>
    /// Sets the node type for elements.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <returns>The reference to the current builder.</returns>
    public FiniteElementBuilder SetNodesType(ElementNodeType nodeType)
    {
        if (Nodes.Count != 0)
        {
            throw new FeaException("You have to remove all nodes before changing node type.");
        }

        NodeType = nodeType;
        return this;
    }

    /// <summary>
    /// Adds 1D node.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <returns>The reference to the current builder.</returns>
    /// <exception cref="FeaException">Raise an exception if builder dimension is not 1D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x)
    {
        if (NodeType.Dimension != Dimensional.OneDimensional)
        {
            throw new FeaException($"The current accepted node type is {NodeType.Dimension.ToString()}. You should use another method to specify all dimentions.");
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
    /// <exception cref="FeaException">Raise an exception if builder dimension is not 2D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y)
    {
        if (NodeType.Dimension != Dimensional.TwoDimensional)
        {
            throw new FeaException($"The current accepted node type is {NodeType.Dimension.ToString()}. You should use another method to specify all dimentions.");
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
    /// <exception cref="FeaException">Raise an exception if builder dimension is not 3D.</exception>
    public FiniteElementBuilder AddNode(CoordinateValue x, CoordinateValue y, CoordinateValue z)
    {
        if (NodeType.Dimension != Dimensional.ThreeDimensional)
        {
            throw new FeaException($"The current accepted node type is {NodeType.Dimension.ToString()}. You should use another method to specify all dimentions.");
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

    private ElementNodeType NodeType { get; set; } = new();
    
    private ICollection<IElementNode> Nodes { get; set; } = [];
}