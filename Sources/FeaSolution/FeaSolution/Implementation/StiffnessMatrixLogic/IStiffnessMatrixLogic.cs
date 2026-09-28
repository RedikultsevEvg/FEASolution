using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public interface IStiffnessMatrixLogic
{
    /// <summary>
    /// Полный алгоритм получения матрицы жесткости элемента.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="lambda">Коэффициент теплопроводности, W/(m·K)</param>
    /// <param name="thickness">Толщина, м</param>
    StiffnessMatrix GetLocalMatrix(IFiniteElement element, MatrixValue lambda, MatrixValue thickness);
}