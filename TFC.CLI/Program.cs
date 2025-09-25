using TFC.TrainFareCalculator;
using Directory = TFC.TrainFareCalculator.Directory;

namespace TFC.CLI;

internal class Program
{
    public static void Main(string[] args)
    {
        // check if command-line argument is provided
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the path to the fare matrix directory as a command-line argument.");
            return;
        }

        var directory = Directory.Load(args[0]);
        var graph = GraphBuilder.Build(directory);

        do
        {
            try
            {
                var isStoredValue = GetIsStoredValueCard();
                var from = GetStartTransitLineAndStation(directory);
                var to = GetDestTransitLineAndStation(directory);

                PrintFareAndPath(from, to, graph, isStoredValue);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine("Press any key to try again...");
                Console.ReadKey();
            }

            Console.Clear();
        } while (true);
    }
    
    private static void PrintTransitLines(List<Matrix> matrices)
    {
        // print available transit lines 
        Console.WriteLine("Available Transit Lines:");
        foreach (var (matrix, i) in matrices.Select((matrix, i) => (matrix, i)))
            Console.WriteLine($"{i + 1}) {matrix.TransitLine}");
    }

    private static void PrintStations(Matrix matrix)
    {
        // print available stations for the selected transit line
        Console.WriteLine($"Stations on {matrix.TransitLine}:");
        foreach (var (station, i) in matrix.Stations.Select((station, i) => (station, i)))
            Console.WriteLine($"{i + 1}) ({station.Code}) {station.Name}");
    }

    private static bool TryGetInput(string prompt, out int result, Predicate<int> validator)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        return int.TryParse(input, out result) && validator(result);
    }

    private static bool GetIsStoredValueCard()
    {
        // let user choose payment type (Stored Value Card or Single Journey Ticket)
        Console.WriteLine("1) Stored Value Card (beep card)");
        Console.WriteLine("2) Single Journey Ticket");
        if (!TryGetInput("Select payment type (number): ", out var paymentType, i => i is 1 or 2))
            throw new InvalidDataException("Invalid payment type selection.");

        Console.Clear();
        return paymentType == 1; // flag to indicate if using stored value card (SVC) or single journey ticket (SJT)
    }

    private static Station GetStartTransitLineAndStation(Directory directory)
    {
        var matrices = directory.Matrices;
        // let user choose starting transit line and station
        PrintTransitLines(matrices);
        if (!TryGetInput("Select starting transit line (number): ", out var startLineIndex,
                i => i >= 1 && i <= matrices.Count))
            throw new InvalidDataException("Invalid transit line selection.");

        Console.Clear();
        var startLine = matrices.ElementAt(startLineIndex - 1);

        // let user choose starting station
        PrintStations(startLine);
        if (!TryGetInput("Select starting station (number): ", out var startStationIndex,
                i => i >= 1 && i <= startLine.Stations.Count))
            throw new InvalidDataException("Invalid station selection.");


        Console.Clear();
        var startStation = startLine.Stations[startStationIndex - 1];

        return startStation;
    }

    private static Station GetDestTransitLineAndStation(Directory directory)
    {
        var matrices = directory.Matrices;
        // let user choose destination transit line and station
        PrintTransitLines(matrices);
        if (!TryGetInput("Select destination transit line (number): ", out var destLineIndex,
                i => i >= 1 && i <= matrices.Count))
            throw new InvalidDataException("Invalid transit line selection.");


        Console.Clear();
        var destLine = matrices.ElementAt(destLineIndex - 1);

        PrintStations(destLine);
        if (!TryGetInput("Select destination station (number): ", out var destStationIndex,
                i => i >= 1 && i <= destLine.Stations.Count))
            throw new InvalidDataException("Invalid station selection.");

        Console.Clear();
        var destStation = destLine.Stations[destStationIndex - 1];

        return destStation;
    }
    private static void PrintFareAndPath(Station from, Station to, Graph graph, bool isStoredValue)
    {
        if (from.Equals(to))
        {
            Console.WriteLine($"Starting and destination stations are the same. Fare is {0:C}");
            return;
        }

        var (sjt, svc) = graph.FindShortestPaths(from, to);

        Console.Clear();

        Console.WriteLine($"Shortest fare from {from} to {to}:");
        var path = isStoredValue ? svc : sjt;

        Console.WriteLine($"Total Fare: {path.Total:C}");
        Console.WriteLine("Path:");
        Console.WriteLine($"- {string.Join(" -> ", path.Path)}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}