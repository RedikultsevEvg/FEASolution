using FeaSolution.Core.Interfaces;

namespace FeaSolution.Core.Types;

public class SolutionItem
{
    public IElementNode Node { get; init; }

    public DegreeOfFreedom DegreeOfFreedom { get; init; }

    public SolutionValue Value { get; init; }
}