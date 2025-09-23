namespace TFC.TrainFareCalculator;

public static class FareMatrixLoader
{
    private const char Delimiter = ',';
    private const string CommentPrefix = "#";

    public static (TransitLine TransitLine, string[] Stations) LoadFromFile(string path, Graph graph)
    {
        var lines = File.ReadAllLines(path);

        TransitLine? transitLine = null; // The transit line for this fare matrix (LRT-1, LRT-2, MRT-3)
        List<string> stations = []; // List of station names

        Dictionary<(string, string), decimal>
            svcFares = [],
            sjtFares = []; // Dictionary to hold fares between station pairs

        var svcParsed = false; // Flag to indicate if SVC fares have been parsed
        var currentStation = 0; // Index of the current station being processed

        foreach (var line in lines)
        {
            // Trim whitespace and skip empty lines or comments
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(CommentPrefix))
                continue;

            // first line should contain the transit line (YL, PL, GL)
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

            // next couple of lines contain station names and fares separated by commas
            var delimited = trimmedLine.Split(Delimiter).Select(s => s.Trim()).ToArray();

            // second line should contain the station names separated by commas
            // the stations are arranged from west to east or south to north
            if (stations.Count == 0)
            {
                stations.AddRange(delimited);
                continue;
            }

            // subsequent lines should contain the fares between stations

            // first, check if the number of fares matches the number of stations
            if (delimited.Length != stations.Count)
                throw new InvalidDataException("Number of fares does not match number of stations.");

            // loop through the SVC fares
            if (!svcParsed && currentStation < stations.Count)
            {
                ParseFareMatrixLine(delimited, currentStation, stations, svcFares);
                currentStation++;
                if (currentStation == stations.Count)
                {
                    svcParsed = true;
                    currentStation = 0; // reset for SJT fares
                }
                continue;
            }

            // loop through the SJT fares
            if (!svcParsed || currentStation >= stations.Count)
                continue;

            ParseFareMatrixLine(delimited, currentStation, stations, sjtFares);
            currentStation++;
        }

        // ensure that we have read a transit
        if (!transitLine.HasValue)
            throw new InvalidDataException("Transit line not specified in the fare matrix.");

        // populate the graph with the parsed fares
        if (!svcParsed)
            throw new InvalidDataException("Incomplete fare matrix data.");

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

    public static void LoadTransfersFromFile(string path, Graph graph)
    {
        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(CommentPrefix))
                continue;
            var parts = trimmedLine.Split(Delimiter).Select(s => s.Trim()).ToArray();
            if (parts.Length != 4)
                throw new InvalidDataException($"Invalid transfer line format: {trimmedLine}");

            var fromStation = parts[0];
            var fromLine = parts[1] switch
            {
                "YL" => TransitLine.YellowLine,
                "PL" => TransitLine.PurpleLine,
                "GL" => TransitLine.GreenLine,
                _ => throw new InvalidDataException($"Invalid transit line identifier: {parts[1]}")
            };

            var toStation = parts[2];
            var toLine = parts[3] switch
            {
                "YL" => TransitLine.YellowLine,
                "PL" => TransitLine.PurpleLine,
                "GL" => TransitLine.GreenLine,
                _ => throw new InvalidDataException($"Invalid transit line identifier: {parts[3]}")
            };

            var fromId = new StationId(fromLine, fromStation);
            var toId = new StationId(toLine, toStation);

            // Assuming transfers have zero fare
            graph.AddTransfer(fromId, toId);
        }
    }

    private static void ParseFareMatrixLine(string[] delimited, int current, IReadOnlyList<string> stations, Dictionary<(string, string), decimal> fares)
    {
        var from = stations[current];
        foreach (var (to, index) in stations.Select((name, index) => (name, index)))
        {
            var fareValue = delimited[index];
            if (!decimal.TryParse(fareValue, out var fare))
                throw new InvalidDataException($"Invalid fare value: {fareValue}");
            fares[(from, to)] = fare;
        }
    }
}