using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.ElementNodes;

namespace FeaSolution.Implementation.Builders;

public class SolutionBuilder
{
    /// <summary>
    /// Creates a construction model solution.
    /// </summary>
    /// <returns>The new construction model solution.</returns>
    public ConstructionModelSolution Build()
    {
        // Demo data should be replaced later with production code.

        var degree = new DegreeOfFreedom
        {
            Freedom = Freedom.Temperature
        };

        return new ConstructionModelSolution
        {
            Items =
            [
                new SolutionItem
                {
                    DegreeOfFreedom = degree,
                    Node = new ElementNode(ElementNodeType.Type2D)
                    {
                        X = 0,
                        Y = 1
                    },
                    Value = 20
                },
                new SolutionItem
                {
                    DegreeOfFreedom = degree,
                    Node = new ElementNode(ElementNodeType.Type2D)
                    {
                        X = 2,
                        Y = 2
                    },
                    Value = 60
                },
                new SolutionItem
                {
                    DegreeOfFreedom = degree,
                    Node = new ElementNode(ElementNodeType.Type2D)
                    {
                        X = 2,
                        Y = 0
                    },
                    Value = 60
                },
                new SolutionItem
                {
                    DegreeOfFreedom = degree,
                    Node = new ElementNode(ElementNodeType.Type2D)
                    {
                        X = 4,
                        Y = 1
                    },
                    Value = 100
                }
            ]
        };
    }

    /// <summary>
    /// Sets the construction model.
    /// </summary>
    /// <param name="model">Construction model.</param>
    /// <returns>The reference to the current builder.</returns>
    public  SolutionBuilder SetModel(IConstructionModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return this;
    }

    /// <summary>
    /// Calculate construction model stiffness matrix.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder Assembly() => this;

    /// <summary>
    /// Validates for the minimum count of element. The minimum is 2.
    /// If less then 2 - throw an exception.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForCountOfElement() => this;

    /// <summary>
    /// Validates if there is at least one common node in model.
    /// If no - throw an exception.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForCommonElements() => this;

    /// <summary>
    /// Validates for the zero square elements.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForZeroSquareElements() => this;

    /// <summary>
    /// Validates for the quality of elements.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForQualityOfElements() => this;
    
    /// <summary>
    /// Gets the zero square elements.
    /// </summary>
    /// <returns>The collection of elements.</returns>
    public ICollection<IFiniteElement> GetZeroSquareElements() => [];

    /// <summary>
    /// Gets not quality elements.
    /// </summary>
    /// <returns>The collection of elements.</returns>
    public ICollection<IFiniteElement> GetNotQualityOfElements() => [];
}