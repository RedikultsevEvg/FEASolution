namespace FeaSolution.Core.Interfaces;

/// <summary>
/// Stiffness matrix.
/// </summary>
public interface IStiffnessMatrix
{
    /// <summary>
    /// Gets or sets the matrix value by pair of nodes.<br/>
    /// Support symmentric:  matrix[A,B] == matrix[B,A].
    /// </summary>
    /// <param name="firstNode">First node.</param>
    /// <param name="secondNode">Second node.</param>
    /// <returns></returns>
    MatrixValue this[IElementNode firstNode, IElementNode secondNode] { get; set; }

    /// <summary>
    /// Returns non-zero values.
    /// Every symmetric pair (i, j) exist one time in result.
    /// </summary>
    IEnumerable<(IElementNode I, IElementNode J, MatrixValue Value)> NonZeroMatrixValues();

    // Returns registered nodes.
    IReadOnlyList<IElementNode> Nodes { get; }

    /// <summary>
    /// Add value to existing matrix value by pair of nodes.
    /// </summary>
    /// <param name="firstNode">First node.</param>
    /// <param name="secondNode">Second node.</param>
    /// <param name="valueToAdd">Value to be added.</param>
    void AddValue(IElementNode firstNode, IElementNode secondNode, MatrixValue valueToAdd);
}