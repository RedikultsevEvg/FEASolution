namespace FeaSolution.Core.Types;

public class FiniteElementType
{
    public required ICollection<DegreeOfFreedom> Freedoms { get; init; }

    public required ElementNodeType NodeType { get; init; }

    public required Action<MatrixValue> StiffnessMatrixCalculationMethod { get; init; }
}