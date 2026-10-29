using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.DirichletCalculation;
using FeaSolution.Implementation.StiffnessMatrixLogic;
using FeaSolution.Implementation.StiffnessMatrixs;
using FeaSolution.MathNet.SleCalculation;

namespace FeaSolution.Implementation.Builders;

/// <summary>
/// Solution builder.
/// </summary>
/// <param name="constructionModel">Construction model.</param>
public class SolutionBuilder(IConstructionModel constructionModel)
{
    private IStiffnessMatrix? StiffnessMatrix { get; set; }

    private ICollection<DirichletCondition> DirichletConditions { get; set; } = [];

    private readonly IDirichletLogic _dirichletLogic = new DirichletLogic();
    private readonly ISleCalculator _sleCalculator = new CholeskySleCalculator();

    /// <summary>
    /// Creates a construction model solution.
    /// </summary>
    /// <returns>The new construction model solution.</returns>
    public ConstructionModelSolution Build()
    {
        ArgumentNullException.ThrowIfNull(constructionModel);
        ArgumentNullException.ThrowIfNull(DirichletConditions);
        FeaSolutionBuilderException.ThrowIfTrue(StiffnessMatrix is null, $"Construction model stiffness matrix is not assembled. Call '{nameof(Assembly)}' first.");
        FeaSolutionBuilderException.ThrowIfTrue(DirichletConditions.Count == 0, $"Dirichlet condition collection is empty. Call '{nameof(AddDirichlet)}' first.");

        var vectorF = GetVector(StiffnessMatrix!);

        _dirichletLogic.ApplyDirichlet(StiffnessMatrix!, vectorF, DirichletConditions);
        
        var vectorT = _sleCalculator.SolveSLE(StiffnessMatrix!, vectorF);

        return new ConstructionModelSolution
        {
            Items = vectorT
        };
    }

    /// <summary>
    /// Calculate construction model stiffness matrix.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder Assembly(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(constructionModel);
        ArgumentNullException.ThrowIfNull(constructionModel.Elements);
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel.CommonFreedoms == null! || constructionModel.CommonFreedoms.Count == 0,
            "The freedom for solution is not defined.");

        var freedoms = constructionModel.CommonFreedoms;
        var elements = constructionModel.Elements;

        var localMatrices = new IStiffnessMatrix[elements.Count];

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        Parallel.For(
            fromInclusive: 0,
            toExclusive: elements.Count,
            options,
            i =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                var finiteElement = elements.ElementAt(i);
                var logic = StiffnessMatrixLogicSelector.GetLogic(
                    freedoms,
                    finiteElement.Type.NodeType.Dimension,
                    finiteElement.Nodes.Count);

                localMatrices[i] = logic.GetMatrix(finiteElement);
            });

        IStiffnessMatrix globalStiffnessMatrix = StiffnessMatrixFactory.CreateNew();
        foreach (var localMatrix in localMatrices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            globalStiffnessMatrix.AddMatrix(localMatrix);
        }

        StiffnessMatrix = globalStiffnessMatrix;

        return this;
    }

    /// <summary>
    /// Adds Direchlet condition.
    /// </summary>
    /// <param name="nodeId">Node id.</param>
    /// <param name="freedom">Freedom.</param>
    /// <param name="conditionValue">Condition value.</param>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder AddDirichlet(string nodeId, Freedom freedom, MatrixValue conditionValue)
    {
        FeaCommonException.ThrowIfTrue(constructionModel == null!, "Can't validate the condition - the construction model is null.");
        FeaCommonException.ThrowIfTrue(constructionModel?.Elements == null 
                                       || constructionModel.Elements.Count == 0 , "Can't validate the condition - the construction model is empty (element collection is null or empty).");

        var nodes = constructionModel?.Elements.SelectMany(e => e.Nodes);
        var node = nodes?.FirstOrDefault(n => n.NodeId == nodeId);
        FeaCommonException.ThrowIfTrue(node == null, $"Can't find the node with id: '{nodeId}' in construction model.");

        DirichletConditions.Add(new DirichletCondition
        {
            Node = node!,
            Freedom = freedom,
            Value = conditionValue
        });

        return this;
    }

    /// <summary>
    /// Validates for the minimum count of element. The minimum is 2.
    /// If less then 2 - throw an exception.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForCountOfElement()
    {
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel == null!,"The construction model is not defined (equal null).");
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel!.Elements == null!,
            "The construction model element collection is not defined (equal null).");

        var elementCount = constructionModel.Elements!.Count;
        FeaSolutionBuilderException.ThrowIfTrue(elementCount < 2, "The construction model contains not enough elements (less then two).");

        return this;
    }

    /// <summary>
    /// Validates if there is at least one common node in model.
    /// If no - throw an exception.
    /// </summary>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder ValidateForCommonElements()
    {
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel == null!,
            "The construction model is not defined (equal null).");
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel!.Elements == null!,
            "The construction model element collection is not defined (equal null).");
        FeaSolutionBuilderException.ThrowIfTrue(constructionModel.Elements!.Count == 0,
            "The construction model element collection is empty.");

        var hasCommonNode = constructionModel
            .Elements
            .SelectMany(e => e.Nodes)
            .GroupBy(n => n, ReferenceEqualityComparer.Instance)
            .Any(g => g.Count() > 1);

        FeaSolutionBuilderException.ThrowIfTrue(!hasCommonNode,
            "The construction model has no common nodes. All elements are isolated, " +
            "which makes the solution physically meaningless.");

        return this;
    }

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
    public SolutionBuilder ValidateAll()
    {
        return this
            .ValidateForCountOfElement()
            .ValidateForCommonElements();
    }
    
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

    private SolutionItem[] GetVector(IStiffnessMatrix stiffnessMatrix)
    {
        ArgumentNullException.ThrowIfNull(stiffnessMatrix);

        var nodes = stiffnessMatrix.Nodes;
        var vector = nodes
            .Select(node => new SolutionItem
            {
                Freedom = Freedom.Temperature,
                Node = node,
                Value = 0
            }).ToArray();

        return vector;
    }
}