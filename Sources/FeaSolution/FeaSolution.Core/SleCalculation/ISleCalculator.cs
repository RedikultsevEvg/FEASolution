using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Core.SleCalculation;

public interface ISleCalculator
{
    SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet);
}