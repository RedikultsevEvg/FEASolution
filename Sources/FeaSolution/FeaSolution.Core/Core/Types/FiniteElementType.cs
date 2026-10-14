using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Enums;
using FeaSolution.Core.Types;

namespace FeaSolution.Core.Core.Types;

/// <summary>
/// Finite element type.
/// </summary>
public class FiniteElementType
{
    /// <summary>
    /// Collection of pairs: freedom and options.
    /// </summary>
    public required ICollection<(Freedom, IElementOptions)> Freedoms { get; init; }

    /// <summary>
    /// Finite element node type.
    /// </summary>
    public required ElementNodeType NodeType { get; init; }
}