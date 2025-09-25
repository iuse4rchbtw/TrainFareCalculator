namespace TFC.TrainFareCalculator;

/// <summary>
/// Utility class for loading fare matrices and transfer definitions from text files.
/// Expected file format (example):
///   Line 1: Transit line code (GL | PL | YL)
///   Line 2: Comma-separated station names
///   Next N lines: SVC fare matrix (N rows, each with N values)
///   Next N lines: SJT fare matrix (N rows, each with N values)
///
/// Transfers file format (transfers.txt):
///   Each non-comment line: FromStation, FromLineCode, ToStation, ToLineCode
///   (All transfers assumed zero fare.)
/// </summary>
public static class FareMatrixLoader
{
    private const char Delimiter = ',';
    private const string CommentPrefix = "#";

    /// <summary>
    /// Loads a single transit line fare matrix from a file and populates the graph
    /// with edges (both SJT and SVC fares). Assumes a fully connected (complete) matrix.
    /// </summary>
    /// <param name="path">Path to the fare matrix file.</param>
    /// <param name="graph">Graph to populate with edges.</param>
    /// <returns>Tuple of the transit line and ordered station names.</returns>
    /// <exception cref="InvalidDataException">On malformed or incomplete input.</exception>
    public static (TransitLine TransitLine, string[] Stations) LoadMatrixFromFile(string path, Graph graph)
    {
        var lines = File.ReadAllLines(path);

        TransitLine? transitLine = null;
        List<string> stations = [];

        // Fare stores keyed by (fromStationName, toStationName)
        Dictionary<(string, string), decimal> svcFares = [];
        Dictionary<(string, string), decimal> sjtFares = [];

        var svcParsed = false;      // Indicates we've finished the SVC matrix section
        var currentStation = 0;     // Row index tracker

        foreach (var raw in lines)
        {
            var trimmedLine = raw.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(CommentPrefix))
                continue; // Skip blank/comment lines

            // First meaningful line identifies the transit line
            if (!transitLine.HasValue)
            {
                transitLine = trimmedLine switch
                {
                    "YL" => TransitLine.YellowLine,
                    "PL" => TransitLine.PurpleLine,
                    "GL" => TransitLine.GreenLine,
                    _ => throw new InvalidDataException($"Invalid transit line identifier: {trimmedLine}")
                };
                continue;
            }

            // Remaining lines are either station names line or fare matrix rows
            var delimited = trimmedLine.Split(Delimiter).Select(s => s.Trim()).ToArray();

            // Second meaningful line: station names
            if (stations.Count == 0)
            {
                stations.AddRange(delimited);
                continue;
            }

            // All subsequent lines are fare rows
            if (delimited.Length != stations.Count)
                throw new InvalidDataException("Number of fares does not match number of stations.");

            // First pass: SVC matrix rows
            if (!svcParsed && currentStation < stations.Count)
            {
                ParseFareMatrixLine(delimited, currentStation, stations, svcFares);
                currentStation++;

                // Completed SVC block
                if (currentStation == stations.Count)
                {
                    svcParsed = true;
                    currentStation = 0; // Reset for SJT rows
                }
                continue;
            }

            // Second pass: SJT matrix rows
            if (!svcParsed || currentStation >= stations.Count)
                continue; // Should not occur unless format invalid

            ParseFareMatrixLine(delimited, currentStation, stations, sjtFares);
            currentStation++;
        }

        if (!transitLine.HasValue)
            throw new InvalidDataException("Transit line not specified in the fare matrix.");

        if (!svcParsed || currentStation != stations.Count)
            throw new InvalidDataException("Incomplete fare matrix data.");

        // Populate graph with parsed fares
        foreach (var key in svcFares.Keys)
        {
            var (from, to) = key;
            var svcFare = svcFares[key];
            var sjtFare = sjtFares[key];

            var fromId = new StationId(transitLine.Value, from);
            var toId = new StationId(transitLine.Value, to);

            graph.AddEdge(fromId, toId, new FareInfo(svcFare, sjtFare));
        }

        return (transitLine.Value, stations.ToArray());
    }

    /// <summary>
    /// Loads transfer definitions (zero-fare connections) from a file.
    /// </summary>
    /// <param name="path">Path to transfers file.</param>
    /// <param name="graph">Graph to augment with transfer edges.</param>
    /// <exception cref="InvalidDataException">On malformed lines.</exception>
    public static void LoadTransfersFromFile(string path, Graph graph)
    {
        // Read entire file into memory (files are expected to be small).
        var lines = File.ReadAllLines(path);

        foreach (var raw in lines)
        {
            // Normalize whitespace and ignore empty or comment lines.
            var trimmedLine = raw.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(CommentPrefix))
                continue;

            // Each valid line must contain exactly four comma-separated tokens:
            //   FromStation, FromLineCode, ToStation, ToLineCode
            var parts = trimmedLine.Split(Delimiter).Select(s => s.Trim()).ToArray();
            if (parts.Length != 4)
                throw new InvalidDataException($"Invalid transfer line format: {trimmedLine}");

            // Parse origin station + line code.
            var fromStation = parts[0];
            var fromLine = parts[1] switch
            {
                "YL" => TransitLine.YellowLine,
                "PL" => TransitLine.PurpleLine,
                "GL" => TransitLine.GreenLine,
                _ => throw new InvalidDataException($"Invalid transit line identifier: {parts[1]}")
            };

            // Parse destination station + line code.
            var toStation = parts[2];
            var toLine = parts[3] switch
            {
                "YL" => TransitLine.YellowLine,
                "PL" => TransitLine.PurpleLine,
                "GL" => TransitLine.GreenLine,
                _ => throw new InvalidDataException($"Invalid transit line identifier: {parts[3]}")
            };

            // Construct strongly-typed station identifiers.
            var fromId = new StationId(fromLine, fromStation);
            var toId = new StationId(toLine, toStation);

            // Register a bidirectional zero-cost edge. (Graph.AddTransfer enforces that
            // both stations already exist; if they don't, an exception will surface here,
            // signaling either a data ordering issue or a malformed transfer list.)
            graph.AddTransfer(fromId, toId);
        }
    }

    /// <summary>
    /// Parses a single row of a square fare matrix and records fares for (fromRow, each toColumn).
    /// </summary>
    /// <param name="delimited">Row values as strings.</param>
    /// <param name="current">Row index (origin station index).</param>
    /// <param name="stations">Ordered station list.</param>
    /// <param name="fares">Dictionary to populate with parsed fares.</param>
    /// <exception cref="InvalidDataException">On invalid numeric values.</exception>
    private static void ParseFareMatrixLine(
        string[] delimited,
        int current,
        IReadOnlyList<string> stations,
        Dictionary<(string, string), decimal> fares)
    {
        var from = stations[current];
        // Each value corresponds to a fare from 'from' to the station at the same index.
        foreach (var (to, index) in stations.Select((name, index) => (name, index)))
        {
            var fareValue = delimited[index];
            if (!decimal.TryParse(fareValue, out var fare))
                throw new InvalidDataException($"Invalid fare value: {fareValue}");
            fares[(from, to)] = fare;
        }
    }
}