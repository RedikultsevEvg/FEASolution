using FeaSolution.Core.Interfaces;

namespace FeaSolution.Implementation.StiffnessMatrixLogic;

public interface IStiffnessMatrixLogic
{
    /// <summary>
    /// Полный алгоритм получения матрицы жесткости элемента.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="thermalConductivity">Коэффициент теплопроводности, W/(m·K)</param>
    /// <param name="thickness">Толщина, м</param>
    IStiffnessMatrix GetMatrix(IFiniteElement element, MatrixValue thermalConductivity, MatrixValue thickness);
}