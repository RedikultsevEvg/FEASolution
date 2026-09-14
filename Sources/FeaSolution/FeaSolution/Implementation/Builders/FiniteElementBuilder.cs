using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.FiniteElements;

namespace FeaSolution.Implementation.Builders;

public static class FiniteElementBuilder
{
    /// <summary>
    /// Creates a new finite element 1D.
    /// </summary>
    /// <returns>A new 1D node type.</returns>
    public static IFiniteElement CreateNew1D(string userId = "")
    {
        var elementType1D = new FiniteElementType
        {
            NodeType = ElementNodeType.Type1d,
            Freedoms =
            [
                new DegreeOfFreedom()
            ],
            StiffnessMatrixCalculationMethod = null!
        };

        var newElement = new FiniteElement(userId, elementType1D);

        return newElement;
    }
}