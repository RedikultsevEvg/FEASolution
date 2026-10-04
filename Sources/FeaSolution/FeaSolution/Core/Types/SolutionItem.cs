using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;

namespace FeaSolution.Core.Types;

public class SolutionItem
{
    public IElementNode Node { get; init; }

    public Freedom DegreeOfFreedom { get; init; }

    public SolutionValue Value { get; init; }
}