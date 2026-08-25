namespace FeaSolution.Core.Interfaces;

public interface IFiniteElementStiffnessMatrix
{
    ICollection<IStiffnessMatrixValue> Values { get; }
}