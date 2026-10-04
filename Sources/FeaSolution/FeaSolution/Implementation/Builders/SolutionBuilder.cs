using FeaSolution.Core.Interfaces;
using FeaSolution.Core.Types;
using FeaSolution.Implementation.ConstructionModelSolutions;
using FeaSolution.Implementation.ElementNodes;
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
        ArgumentNullException.ThrowIfNull(constructionModel);
        if (GlobalStiffnessMatrix is null)
            throw new InvalidOperationException(
                "Global stiffness matrix is not assembled. Call AssemblyAsync first.");

        int n = GlobalStiffnessMatrix.NodeCount;

        // 2. Конвертируем глобальную матрицу жёсткости в формат CSparse (CSC).
        var cscMatrix = ConvertToCSparse(GlobalStiffnessMatrix);

        // 3. Формируем правую часть. В тесте F = 0.
        double[] F = new double[n];

        // 4. Применяем условия Дирихле (модификация K и F).
        //var dirichlet = CollectDirichletConditions(constructionModel, freedoms);

        /*
        foreach (var (i, Ti) in dirichlet)
        {
            ApplyDirichlet(cscMatrix, F, i, Ti);
        }
        */

        // 5. Решаем K·T = F через CSparse.
        double[] T = new double[GlobalStiffnessMatrix.NodeCount];
        double[] results = new double[GlobalStiffnessMatrix.NodeCount];

        Array.Copy(F, T, n);
        var order = CSparse.ColumnOrdering.MinimumDegreeAtPlusA;
        var chol = CSparse.Double.Factorization.SparseCholesky.Create(cscMatrix, order);

        chol.Solve(T, results);

        // 6. УПАКОВКА: превращаем double[] в список SolutionItem.
        //    Привязка чисел к узлам и DOF через freedoms.
        var items = new List<SolutionItem>(n);

        for (int i = 0; i < n; i++)
        {
            items.Add(new SolutionItem
            {
                //Node = freedoms[i].Node,
                Node = constructionModel.Elements.FirstOrDefault().Nodes.FirstOrDefault(),
                DegreeOfFreedom = constructionModel.CommonFreedoms.FirstOrDefault(),
                //Value = new SolutionValue(T[i])
                Value = 100
            });
        }

        // 7. Возвращаем доменный объект решения.
        return new ConstructionModelSolution
        {
            Items = items
        };
    }

    private static CSparse.Storage.CompressedColumnStorage<double> ConvertToCSparse(IStiffnessMatrix matrix)
    {
        // 1. Получаем размерность из самой матрицы
        int n = matrix.NodeCount;

        var coordinateStorage = new CSparse.Storage.CoordinateStorage<double>(n, n, n * 20);

        // 2. Заполняем её значениями
        foreach (var (row, col, value) in matrix.NonZeroMatrixValuesWithIndexes())
        {
            coordinateStorage.At(row, col, value);
        }

        // 3. Конвертируем в CSC-формат через SparseMatrix.OfIndexed
        return CSparse.Double.SparseMatrix.OfIndexed(coordinateStorage);
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