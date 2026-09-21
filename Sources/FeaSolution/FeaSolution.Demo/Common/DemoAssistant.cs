using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.Demo.Common;

internal static class DemoAssistant
{
    public static void ConsoleWriteExampleResults(string methodName, ConstructionModelSolution solution)
    {
        var solutionItemsGroupedByFreedom = solution.Items.GroupBy(i => i.DegreeOfFreedom);

        Console.WriteLine("=========================================");
        Console.WriteLine($"Demo of {methodName}");
        foreach (var group in solutionItemsGroupedByFreedom)
        {
            Console.WriteLine();
            Console.WriteLine("Next solution values for degree of freedom = '{0}'", group.Key.Freedom.ToString());

            foreach (var solutionItem in group)
            {
                Console.WriteLine(
                    "  Node: X = {0}, Y = {1}, Z = {2} ; Value = {3}",
                    solutionItem.Node.X,
                    solutionItem.Node.Y,
                    solutionItem.Node.Z,
                    solutionItem.Value);
            }
        }
        Console.WriteLine();
        Console.WriteLine("=========================================");
    }
}