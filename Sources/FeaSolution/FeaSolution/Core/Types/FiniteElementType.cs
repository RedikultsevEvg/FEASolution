using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

/// <summary>
/// Finite element type.
/// </summary>
public class FiniteElementType
{
    /// <summary>
    /// Collection of degree of freedom.
    /// </summary>
    public required ICollection<Freedom> Freedoms { get; init; }

    /// <summary>
    /// Finite element node type.
    /// </summary>
    public required ElementNodeType NodeType { get; init; }

    public required Action<MatrixValue> StiffnessMatrixCalculationMethod { get; init; }
}