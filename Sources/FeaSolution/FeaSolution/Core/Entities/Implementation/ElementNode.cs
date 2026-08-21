using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Core.Entities.Implementation;

// <inheritdoc />
public class ElementNode(ElementNodeType type) : IElementNode
{
    // <inheritdoc />
    public ElementNodeType Type { get; private set; } = type;

    // <inheritdoc />
    public double X { get; set; }

    // <inheritdoc />
    public double Y
    {
        get => Type.Dimension == DimensionalType.OneDimensional 
            ? 0 
            : field;
        set
        {
            if (Type.Dimension == DimensionalType.OneDimensional)
            {
                throw new InvalidOperationException("Cannot set Y coordinate for one-dimensional node.");
            }
            field = value;
        }
    }

    // <inheritdoc />
    public double Z
    {
        get => Type.Dimension is DimensionalType.OneDimensional or DimensionalType.TwoDimensional
            ? 0
            : field;
        set
        {
            field = Type.Dimension switch
            {
                DimensionalType.OneDimensional => throw new InvalidOperationException(
                    "Cannot set Z coordinate for one-dimensional node."),
                DimensionalType.TwoDimensional => throw new InvalidOperationException(
                    "Cannot set Z coordinate for two-dimensional node."),
                _ => value
            };
        }
    }
}