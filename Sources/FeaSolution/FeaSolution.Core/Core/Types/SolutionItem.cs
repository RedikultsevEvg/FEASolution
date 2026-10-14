using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

public class SolutionItem
{
    public IElementNode Node { get; init; }

    public Freedom Freedom { get; init; }

    public SolutionValue Value { get; set; }
}