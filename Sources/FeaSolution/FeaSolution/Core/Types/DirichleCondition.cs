using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;

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