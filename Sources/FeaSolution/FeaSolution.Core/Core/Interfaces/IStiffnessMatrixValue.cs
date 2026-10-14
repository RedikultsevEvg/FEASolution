using FeaSolution.Core.Types;

namespace FeaSolution.Core.Core.Interfaces;

public interface IStiffnessMatrixValue
{
    public DegreeOfFreedom DegreeOfFreedom { get; set; }

    public IElementNode Node1 { get; set; }

    public IElementNode Node2 { get; set; }

    public MatrixValue CurrentValue { get; set; }
}