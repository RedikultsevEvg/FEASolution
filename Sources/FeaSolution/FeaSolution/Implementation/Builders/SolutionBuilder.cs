using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.FiniteElements;
using FeaSolution.Implementation.StiffnessMatrixLogic;

namespace FeaSolution.Implementation.Builders;

/// <summary>
/// Solution builder.
/// </summary>
/// <param name="constructionModel">Construction model.</param>
public class SolutionBuilder(IConstructionModel constructionModel)
{
    private IFiniteElementStiffnessMatrix _stiffnessMatrix = new FiniteElementStiffnessMatrix();

    /// <summary>
    /// Creates a construction model solution.
    /// </summary>
    /// <returns>The new construction model solution.</returns>
    public ConstructionModelSolution Build()
    {
        // Demo data should be replaced later with production code.
        ArgumentNullException.ThrowIfNull(constructionModel);

        var degree = new DegreeOfFreedom
        {
            Freedom = Freedom.Temperature
        };

        var items = constructionModel.Elements.SelectMany(elem => 
            elem.Nodes.Select(node => new SolutionItem
        {
            DegreeOfFreedom = degree,
            Node = node,
            Value = 20
        })).ToArray();

        return new ConstructionModelSolution
        {
            Items = items,
        };
    }

    /// <summary>
    /// Calculate construction model stiffness matrix.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder Assembly()
    {
        ArgumentNullException.ThrowIfNull(constructionModel);
        ArgumentNullException.ThrowIfNull(constructionModel.Elements);
        
        var freedoms = constructionModel.Freedoms;
        var elements = constructionModel.Elements;

        foreach (var finiteElement in elements)
        {
            // todo: упростить - передаем только элемент
            var logic = StiffnessMatrixLogicFactory.GetLogic(freedoms, finiteElement.Type.NodeType.Dimension,
                finiteElement.Nodes.Count);

            // todo: Здесь нужно передавать опции (материал и геометрические параметры)
            var localMatrix  = logic.GetLocalMatrix(finiteElement, 4.0, 2.0);
        }


        _stiffnessMatrix = new FiniteElementStiffnessMatrix();

        /*if (allNodes.Any())
        {
            _stiffnessMatrix.Values.Add(new StiffnessMatrixValue
            {
                DegreeOfFreedom = new DegreeOfFreedom(),
                Node1 = allNodes[0],
                Node2 = allNodes[1],
                CurrentValue = 1.1
            });
        }*/

        return this;
    }

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
    /// Does all available validations.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateAll() => this;
    
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