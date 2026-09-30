using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public interface IStiffnessMatrixLogic
{
    /// <summary>
    /// Полный алгоритм получения матрицы жесткости элемента.
    /// </summary>
    /// <param name="element"></param>
    IStiffnessMatrix GetMatrix(IFiniteElement element);
}