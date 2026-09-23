using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.FiniteElements;

public class FiniteElementStiffnessMatrix : IFiniteElementStiffnessMatrix
{
    /// <inheritdoc />
    public ICollection<IStiffnessMatrixValue> Values { get; }
}