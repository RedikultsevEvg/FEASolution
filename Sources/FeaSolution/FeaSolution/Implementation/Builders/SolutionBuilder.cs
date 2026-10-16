using FeaSolution.Core.Enums;
using FeaSolution.Core.Exceptions;
using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.DirichletCalculation;
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

    private ICollection<DirichleCondition> DirichleConditions { get; set; } = [];

    /// <summary>
    /// Creates a construction model solution.
    /// </summary>
    /// <returns>The new construction model solution.</returns>
    public ConstructionModelSolution Build()
    {
        ArgumentNullException.ThrowIfNull(constructionModel);
        if (GlobalStiffnessMatrix is null)
            throw new InvalidOperationException(
                $"Global stiffness matrix is not assembled. Call {nameof(AssemblyAsync)} first.");

        int size = GlobalStiffnessMatrix.NodeCount;

        // 3. Формируем правую часть. В тесте F = 0.
        var vectorF = new SolutionItem[size];

        // 4. Применяем условия Дирихле (модификация K и F).
        IDirichletLogic dirichletLogic = null!;
        dirichletLogic.ApplyDirichlet(GlobalStiffnessMatrix, vectorF, DirichleConditions);

        // 5. Решаем K·T = F через CSparse.
        ISleCalculator sleCalculator = null!;
        SolutionItem[] vectorT = sleCalculator.SolveSLE(GlobalStiffnessMatrix, vectorF);

        return new ConstructionModelSolution
        {
            Items = vectorT
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
    /// Adds Direchle condition.
    /// </summary>
    /// <param name="noteId">Node id.</param>
    /// <param name="freedom">Freedom.</param>
    /// <param name="conditionValue">Condition value.</param>
    /// <returns>The reference to the current builder.</returns>
    public SolutionBuilder AddDirichle(string noteId, Freedom freedom, MatrixValue conditionValue)
    {
        FeaCommonException.ThrowIfTrue(constructionModel == null!, "Can't validate the condition - the construction model is null.");
        FeaCommonException.ThrowIfTrue(constructionModel?.Elements == null || constructionModel?.Elements.Count == 0 , "Can't validate the condition - the construction model is empty (element collection is null or empty).");

        var nodes = constructionModel?.Elements.SelectMany(e => e.Nodes);
        var node = nodes?.FirstOrDefault(n => n.UserId == noteId);
        FeaCommonException.ThrowIfTrue(node == null, $"Can't find the node with id: '{noteId}' in construction model.");

        DirichleConditions.Add(new DirichleCondition
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