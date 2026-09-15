using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.ElementNodes;

/// <inheritdoc cref="IElementNode" />
public class ElementNode(ElementNodeType type) : IElementNode
{
    /// <inheritdoc />
    public ElementNodeType Type { get; } = type;

    /// <inheritdoc />
    public CoordinateValue X { get; set; }

    /// <inheritdoc />
    public CoordinateValue Y
    {
        get => Type.Dimension == Dimensional.OneDimensional 
            ? 0 
            : field;
        set
        {
            if (Type.Dimension == Dimensional.OneDimensional)
            {
                throw new FeaCommonException("Cannot set Y coordinate for one-dimensional node.");
            }
            field = value;
        }
    }

    /// <inheritdoc />
    public CoordinateValue Z
    {
        get => Type.Dimension is Dimensional.OneDimensional or Dimensional.TwoDimensional
            ? 0
            : field;
        set
        {
            field = Type.Dimension switch
            {
                Dimensional.OneDimensional => throw new FeaCommonException(
                    "Cannot set Z coordinate for one-dimensional node."),
                Dimensional.TwoDimensional => throw new FeaCommonException(
                    "Cannot set Z coordinate for two-dimensional node."),
                _ => value
            };
        }
    }
}