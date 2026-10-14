using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Core.Core.SleCalculation;

public interface ISleCalculator
{
    SolutionItem[] SolveSLE(IStiffnessMatrix matrixKWithDirichlet, SolutionItem[] vectorFWithDirichlet);
}