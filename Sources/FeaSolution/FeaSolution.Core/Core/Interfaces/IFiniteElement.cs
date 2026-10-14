using FeaSolution.Core.Core.Types;

namespace FeaSolution.Core.Core.Interfaces;

public interface IFiniteElement
{
    /// <summary>
    /// Unique user-defined identifier within the solution.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Finite element type.
    /// </summary>
    FiniteElementType Type { get; }

    /// <summary>
    /// Finite element nodes.
    /// </summary>
    ICollection<IElementNode> Nodes { get; } 
}