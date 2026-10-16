using FeaSolution.Core.Types;

namespace FeaSolution.Core.Interfaces;

public interface ISleCalculator
{
    SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet);
}