using TFC.TrainFareCalculator;

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

        // read the fare matrix files for LRT-1, LRT-2, and MRT-3 from the specified directory
        var directoryPath = args[0];
        var graph = new Graph();
        var fareMatrices = LoadAllMatrices(directoryPath, graph);

        do
        {
            try
            {
                var isStoredValue = GetIsStoredValueCard();
                var from = GetStartTransitLineAndStation(fareMatrices);
                var to = GetDestTransitLineAndStation(fareMatrices);

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

    private static (TransitLine, string[]) LoadMatrix(string directoryPath, TransitLine line, Graph graph)
    {
        // determine the file name based on the transit line and payment type
        var fileName = line switch
        {
            TransitLine.GreenLine => "GL.txt",
            TransitLine.PurpleLine => "PL.txt",
            TransitLine.YellowLine => "YL.txt",
            _ => throw new ArgumentOutOfRangeException(nameof(line), $"Unsupported transit line: {line}")
        };
        var filePath = Path.Combine(directoryPath, fileName); // construct the full file path
        // check if the file exists
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fare matrix file not found: {filePath}");
        // open file
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return FareMatrixLoader.LoadMatrixFromFile(filePath, graph);
    }

    private static Dictionary<TransitLine, string[]> LoadAllMatrices(string directoryPath, Graph graph)
    {
        // dictionary to hold fare matrices for each combination of transit line and payment type
        var filePath = Path.Combine(directoryPath, "transfers.txt"); // construct the full file path

        Dictionary<TransitLine, string[]> fareMatrices = [];

        foreach (var value in Enum.GetValues(typeof(TransitLine)))
        {
            var line = (TransitLine)value;
            var (transitLine, stations) = LoadMatrix(directoryPath, line, graph);
            fareMatrices[transitLine] = stations;
        }

        FareMatrixLoader.LoadTransfersFromFile(filePath, graph);

        // iterate over all combinations of transit lines and payment types
        return fareMatrices;
    }

    private static void PrintTransitLines()
    {
        // print available transit lines 
        Console.WriteLine("Available Transit Lines:");
        foreach (var (line, index) in Enum.GetValues<TransitLine>().Select((line, index) => (line, index + 1)))
            Console.WriteLine($"{index}) {line.GetDescription()}");
    }

    private static void PrintStations(TransitLine transitLine, string[] stations)
    {
        // print available stations for the selected transit line
        Console.WriteLine($"Stations on {transitLine.GetDescription()}:");
        for (var i = 0; i < stations.Length; i++)
            Console.WriteLine($"{i + 1}) {stations[i]}");
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

    private static StationId GetStartTransitLineAndStation(Dictionary<TransitLine, string[]> fareMatrices)
    {
        // let user choose starting transit line and station
        PrintTransitLines();
        if (!TryGetInput("Select starting transit line (number): ", out var startLineIndex,
                i => i >= 1 && i <= fareMatrices.Count))
            throw new InvalidDataException("Invalid transit line selection.");

        Console.Clear();
        var startLine = fareMatrices.Keys.ElementAt(startLineIndex - 1);

        // let user choose starting station
        PrintStations(startLine, fareMatrices[startLine]);
        if (!TryGetInput("Select starting station (number): ", out var startStationIndex,
                i => i >= 1 && i <= fareMatrices[startLine].Length))
            throw new InvalidDataException("Invalid station selection.");


        Console.Clear();
        var startStation = fareMatrices[startLine][startStationIndex - 1];

        return new StationId(startLine, startStation);
    }

    private static StationId GetDestTransitLineAndStation(Dictionary<TransitLine, string[]> fareMatrices)
    {
        // let user choose destination transit line and station
        PrintTransitLines();
        if (!TryGetInput("Select destination transit line (number): ", out var destLineIndex,
                i => i >= 1 && i <= fareMatrices.Count))
            throw new InvalidDataException("Invalid transit line selection.");


        Console.Clear();
        var destLine = fareMatrices.Keys.ElementAt(destLineIndex - 1);

        PrintStations(destLine, fareMatrices[destLine]);
        if (!TryGetInput("Select destination station (number): ", out var destStationIndex,
                i => i >= 1 && i <= fareMatrices[destLine].Length))
            throw new InvalidDataException("Invalid station selection.");

        Console.Clear();
        var destStation = fareMatrices[destLine][destStationIndex - 1];

        return new StationId(destLine, destStation);
    }
    private static void PrintFareAndPath(StationId from, StationId to, Graph graph, bool isStoredValue)
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