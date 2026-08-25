using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ElementNodes;

// <inheritdoc />
public class ElementNode(ElementNodeType type) : IElementNode
{
    // <inheritdoc />
    public ElementNodeType Type { get; } = type;

    // <inheritdoc />
    public double X { get; set; }

    // <inheritdoc />
    public double Y
    {
        get => Type.Dimension == Dimensional.OneDimensional 
            ? 0 
            : field;
        set
        {
            if (Type.Dimension == Dimensional.OneDimensional)
            {
                throw new FeaException("Cannot set Y coordinate for one-dimensional node.");
            }
            field = value;
        }
    }

    // <inheritdoc />
    public double Z
    {
        get => Type.Dimension is Dimensional.OneDimensional or Dimensional.TwoDimensional
            ? 0
            : field;
        set
        {
            field = Type.Dimension switch
            {
                Dimensional.OneDimensional => throw new FeaException(
                    "Cannot set Z coordinate for one-dimensional node."),
                Dimensional.TwoDimensional => throw new FeaException(
                    "Cannot set Z coordinate for two-dimensional node."),
                _ => value
            };
        }
    }
}