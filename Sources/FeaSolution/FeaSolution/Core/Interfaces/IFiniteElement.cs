using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface IFiniteElement
{
    /// <summary>
    /// Unique user-defined identifier within the solution.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Finite element type.
    /// </summary>
    FiniteElementType ElementType { get; }

    /// <summary>
    /// Finite element nodes.
    /// </summary>
    ICollection<IElementNode> Nodes { get; } 

    /// <summary>
    /// Finite element stiffness matrix.
    /// </summary>
    IFiniteElementStiffnessMatrix StiffnessMatrix { get; }
}