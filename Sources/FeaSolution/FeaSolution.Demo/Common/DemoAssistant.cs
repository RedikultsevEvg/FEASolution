using FeaSolution.Implementation.ConstructionModelSolutions;

namespace FeaSolution.Demo.Common;

internal static class DemoAssistant
{
    public static void ConsoleWriteExampleResults(string methodName, ConstructionModelSolution solution)
    {
        var solutionItemsGroupedByFreedom = solution.Items.GroupBy(i => i.Freedom);

        Console.WriteLine("=========================================");
        Console.WriteLine($"Demo of {methodName}");
        int index = 0;
        foreach (var group in solutionItemsGroupedByFreedom)
        {
            Console.WriteLine();
            Console.WriteLine("Next solution values for degree of freedom = '{0}'", group.Key.ToString());

            foreach (var solutionItem in group)
            {
                Console.WriteLine(
                    "  Node {5} '{3}': X = {0}, Y = {1}, Z = {2} ; Value = {4}",
                    solutionItem.Node.X,
                    solutionItem.Node.Y,
                    solutionItem.Node.Z,
                    solutionItem.Node.NodeId,
                    solutionItem.Value,
                    ++index);
            }
        }
        Console.WriteLine();
        Console.WriteLine("=========================================");
    }
}