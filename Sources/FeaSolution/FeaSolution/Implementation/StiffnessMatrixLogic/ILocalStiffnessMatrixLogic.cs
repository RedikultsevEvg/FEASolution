using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public interface ILocalStiffnessMatrixLogic
{
    /// <summary>
    /// Полный алгоритм получения локальной матрицы елемента.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="lambda">Коэффициент теплопроводности, W/(m·K)</param>
    /// <param name="thickness">Толщина, м</param>
    StiffnessMatrix GetLocalMatrix(IFiniteElement element, MatrixValue lambda, MatrixValue thickness);
}