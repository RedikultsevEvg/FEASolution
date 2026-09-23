using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.FiniteElements;

public class StiffnessMatrixValue : IStiffnessMatrixValue
{
    public required DegreeOfFreedom DegreeOfFreedom { get; set; }
    public required IElementNode Node1 { get; set; }
    public required IElementNode Node2 { get; set; }
    public required MatrixValue CurrentValue { get; set; }
}