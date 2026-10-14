using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;

namespace FeaSolution.Implementation.SleCalculation;

public interface ISleCalculator
{
    SolutionItem[] SolveSLE(IStiffnessMatrix globalStiffnessMatrix, SolutionItem[] vectorF);
}