using TFC.TrainFareCalculator;

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
        var fareMatrices = LoadAllFareMatrices(directoryPath, graph);

        // let user choose starting transit line and station
        PrintTransitLines();
        if (!TryGetInput("Select starting transit line (number): ", out var startLineIndex, 
                i => i >= 1 && i <= fareMatrices.Count))
        {
            Console.WriteLine("Invalid selection. Exiting.");
            return;
        }

        Console.Clear();
        var startLine = fareMatrices.Keys.ElementAt(startLineIndex - 1);

        // let user choose starting station
        PrintStations(startLine, fareMatrices[startLine]);
        if (!TryGetInput("Select starting station (number): ", out var startStationIndex, 
                i => i >= 1 && i <= fareMatrices[startLine].Length))
        {
            Console.WriteLine("Invalid selection. Exiting.");
            return;
        }

        Console.Clear();
        var startStation = fareMatrices[startLine][startStationIndex - 1];

        // let user choose destination transit line and station
        PrintTransitLines();
        if (!TryGetInput("Select destination transit line (number): ", out var destLineIndex, 
                i => i >= 1 && i <= fareMatrices.Count))
        {
            Console.WriteLine("Invalid selection. Exiting.");
            return;
        }

        Console.Clear();
        var destLine = fareMatrices.Keys.ElementAt(destLineIndex - 1);

        PrintStations(destLine, fareMatrices[destLine]);
        if (!TryGetInput("Select destination station (number): ", out var destStationIndex, 
                i => i >= 1 && i <= fareMatrices[destLine].Length))
        {
            Console.WriteLine("Invalid selection. Exiting.");
            return;
        }

        Console.Clear();
        var destStation = fareMatrices[destLine][destStationIndex - 1];

        // output the shortest fare between the two selected stations
        var startId = new StationId(startLine, startStation);
        var destId = new StationId(destLine, destStation);

        if (startId.Equals(destId))
        {
            Console.WriteLine($"Starting and destination stations are the same. Fare is {0:C}");
            return;
        }

        var fi = graph.FindShortestPath(startId, destId);

        Console.Clear();

        Console.WriteLine($"Shortest fare from {startId} to {destId}:");
        Console.WriteLine($"  SJT Fare: {fi.SjtFare:C}");
        Console.WriteLine($"  SVC Fare: {fi.SvcFare:C}");
        Console.WriteLine("  Note: Fares are calculated based on the shortest path considering transfers where applicable.");

        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static (TransitLine, string[]) LoadFareMatrix(string directoryPath, TransitLine line, Graph graph)
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
        return FareMatrixLoader.LoadFromFile(filePath, graph);
    }

    private static Dictionary<TransitLine, string[]> LoadAllFareMatrices(string directoryPath, Graph graph)
    {
        // dictionary to hold fare matrices for each combination of transit line and payment type
        var filePath = Path.Combine(directoryPath, "transfers.txt"); // construct the full file path

        Dictionary<TransitLine, string[]> fareMatrices = [];

        foreach (var value in Enum.GetValues(typeof(TransitLine)))
        {
            var line = (TransitLine)value;
            var (transitLine, stations) = LoadFareMatrix(directoryPath, line, graph);
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
}