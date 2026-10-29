using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface IFiniteElement
{
    /// <summary>
    /// Unique user-defined element identifier.
    /// </summary>
    string ElementId { get; }

    /// <summary>
    /// Finite element type.
    /// </summary>
    FiniteElementType Type { get; }

    /// <summary>
    /// Finite element nodes.
    /// </summary>
    ICollection<IElementNode> Nodes { get; }

    /// <summary>
    /// Returns node with relevant Id.
    /// </summary>
    /// <param name="nodeId">Node id.</param>
    /// <returns>Node with relevant Id</returns>
    IElementNode Node(string nodeId);
}