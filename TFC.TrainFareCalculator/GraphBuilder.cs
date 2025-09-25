namespace TFC.TrainFareCalculator;

/// <summary>
/// Builds a Graph (string based) from a Directory definition (matrices + transfers).
/// Assumptions:
///   1. Each Matrix represents ONE transit line (identified by Matrix.LineCode).
///   2. Fares.SingleJourneyTicket and Fares.StoredValueCard are full N×N matrices where
///      entry[i][j] is the total fare from station i to station j on that line (not incremental).
///   3. Transfers list contains zero‑fare links between two stations (possibly different lines).
/// </summary>
public static class GraphBuilder
{
    public static Graph Build(Directory directory)
    {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        var graph = new Graph();

        // 1. Add all intra-line edges from matrices
        foreach (var matrix in directory.Matrices)
            AddMatrix(graph, matrix);

        // 2. Add transfers (zero-fare)
        foreach (var t in directory.Transfers)
        {
            if (t.From is null || t.To is null)
                throw new InvalidDataException("Transfer endpoint missing (From/To).");

            if (string.IsNullOrWhiteSpace(t.From.TransitLine) ||
                string.IsNullOrWhiteSpace(t.To.TransitLine) ||
                string.IsNullOrWhiteSpace(t.From.Code) ||
                string.IsNullOrWhiteSpace(t.To.Code))
                throw new InvalidDataException("Transfer endpoint contains blank values.");

            // consult matrices to get the name of TransitLine/Code
            var fromName = directory.Matrices
                .FirstOrDefault(m => m.TransitLine == t.From.TransitLine)?
                .Stations.FirstOrDefault(s => s.Code == t.From.Code)?.Name;
            var toName = directory.Matrices
                .FirstOrDefault(m => m.TransitLine == t.To.TransitLine)?
                .Stations.FirstOrDefault(s => s.Code == t.To.Code)?.Name;

            if (fromName is null)
                throw new InvalidDataException($"Transfer 'From' station not found: {t.From.TransitLine} {t.From.Code}");

            if (toName is null)
                throw new InvalidDataException($"Transfer 'To' station not found: {t.To.TransitLine} {t.To.Code}");

            var from = new Station(t.From.TransitLine, t.From.Code, fromName);
            var to = new Station(t.To.TransitLine, t.To.Code, toName);

            graph.AddTransfer(from, to);
        }

        return graph;
    }

    private static void AddMatrix(Graph graph, Matrix matrix)
    {
        if (matrix is null) throw new ArgumentNullException(nameof(matrix));
        if (string.IsNullOrWhiteSpace(matrix.TransitLine))
            throw new InvalidDataException("Matrix.LineCode is required (e.g. \"YL\", \"PL\", \"GL\").");
        if (matrix.Stations is null || matrix.Stations.Count == 0)
            throw new InvalidDataException($"Matrix '{matrix.TransitLine}' has no stations.");
        if (matrix.Fares is null)
            throw new InvalidDataException($"Matrix '{matrix.TransitLine}' has null Fares.");

        var sjt = matrix.Fares.SingleJourneyTicket;
        var svc = matrix.Fares.StoredValueCard;
        var n = matrix.Stations.Count;

        ValidateSquareMatrix(sjt, n, $"SJT ({matrix.TransitLine})");
        ValidateSquareMatrix(svc, n, $"SVC ({matrix.TransitLine})");

        // Pre-register stations so transfers referencing them later succeed (optional)
        foreach (var st in matrix.Stations)
            graph.EnsureNode(st);

        // Add undirected edges for every origin/destination pair.
        // Since AddEdge is symmetric we only need i < j to avoid redundant overwrites.
        for (var i = 0; i < n; i++)
        {
            var fromCode = matrix.Stations[i].Code;
            var fromName = matrix.Stations[i].Name;
            for (var j = i + 1; j < n; j++)
            {
                var toCode = matrix.Stations[j].Code;
                var toName = matrix.Stations[j].Name;

                var sjtFare = sjt[i][j];
                var svcFare = svc[i][j];

                var fareInfo = new FareInfo(svcFare, sjtFare);
                var fromId = new Station(matrix.TransitLine, fromCode, fromName);
                var toId = new Station(matrix.TransitLine, toCode, toName);

                graph.AddEdge(fromId, toId, fareInfo);
            }
        }
    }

    private static void ValidateSquareMatrix(List<List<int>>? m, int expected, string label)
    {
        if (m is null)
            throw new InvalidDataException($"{label} matrix is null.");

        if (m.Count != expected)
            throw new InvalidDataException($"{label} row count {m.Count} != station count {expected}.");

        for (var r = 0; r < m.Count; r++)
        {
            if (m[r].Count != expected)
                throw new InvalidDataException($"{label} row {r} has {m[r].Count} columns (expected {expected}).");
        }
    }
}