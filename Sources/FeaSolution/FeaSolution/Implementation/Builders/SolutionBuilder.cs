using FeaSolution.Core.Enums;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using FeaSolution.Implementation.StiffnessMatrixs;

namespace FeaSolution.Implementation.Builders;

/// <summary>
/// Solution builder.
/// </summary>
/// <param name="constructionModel">Construction model.</param>
public class SolutionBuilder(IConstructionModel constructionModel)
{
    public IStiffnessMatrix? GlobalStiffnessMatrix { get; private set; }

    /// <summary>
    /// Creates a construction model solution.
    /// </summary>
    /// <returns>The new construction model solution.</returns>
    public ConstructionModelSolution Build()
    {
        // todo: Demo data should be replaced later with production code.
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

    // todo: вернуть синхронный метод
    /// <summary>
    /// Calculate construction model stiffness matrix.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public async Task<SolutionBuilder> AssemblyAsync(
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(constructionModel);
        ArgumentNullException.ThrowIfNull(constructionModel.Elements);

        var freedoms = constructionModel.CommonFreedoms;
        var elements = constructionModel.Elements;

        var localMatrices = new IStiffnessMatrix[elements.Count];

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: elements.Count,
            options,
            (i, _) =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                var finiteElement = elements.ElementAt(i);
                var logic = StiffnessMatrixLogicSelector.GetLogic(
                    freedoms,
                    finiteElement.Type.NodeType.Dimension,
                    finiteElement.Nodes.Count);

                localMatrices[i] = logic.GetMatrix(finiteElement);
                return ValueTask.CompletedTask;
            });
        
        IStiffnessMatrix globalStiffnessMatrix = StiffnessMatrixFactory.CreateNew();
        foreach (var localMatrix in localMatrices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            globalStiffnessMatrix.AddMatrix(localMatrix);
        }

        GlobalStiffnessMatrix = globalStiffnessMatrix;

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