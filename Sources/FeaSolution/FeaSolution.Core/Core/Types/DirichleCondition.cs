using FeaSolution.Core.Core.Interfaces;
using FeaSolution.Core.Enums;

namespace FeaSolution.Core.Types;

/// <summary>
/// Dirichle condition.
/// </summary>
public class DirichleCondition
{
    public required IElementNode Node { get; init; }

    public required Freedom Freedom { get; init; }

    public required MatrixValue Value { get; init; }
}