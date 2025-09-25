namespace TFC.TrainFareCalculator;

public class Graph
{
    // Adjacency list:
    //   Key:   StationId (origin)
    //   Value: Dictionary where:
    //              Key   = neighboring StationId (destination)
    //              Value = FareInfo (edge weight(s): SJT + SVC)
    private Dictionary<StationId, Dictionary<StationId, FareInfo>> Adjacency { get; } = [];

    public void AddEdge(StationId from, StationId to, FareInfo fareInfo)
    {
        if (!Adjacency.ContainsKey(from))
            Adjacency[from] = [];
            
        Adjacency[from][to] = fareInfo;

        if (!Adjacency.ContainsKey(to))
            Adjacency[to] = [];

        Adjacency[to][from] = fareInfo; // Assuming undirected graph
    }

    public void AddTransfer(StationId from, StationId to)
    {
        // Transfers have no fare associated
        // BUT check if nodes exist before adding links

        if (!Adjacency.ContainsKey(from))
            throw new InvalidOperationException(
                $"Station {from.Station} on line {from.TransitLine} does not exist in the graph.");

        if (!Adjacency.ContainsKey(to))
            throw new InvalidOperationException(
                $"Station {to.Station} on line {to.TransitLine} does not exist in the graph.");

        // Bi‑directional zero cost (transfer)
        Adjacency[to][from] = new FareInfo(0, 0);
        Adjacency[from][to] = new FareInfo(0, 0);
    }

    public record PathResult(decimal Total, IReadOnlyList<StationId> Path);

    /// <summary>
    /// Computes shortest paths (and totals) for SJT and SVC independently.
    /// </summary>
    public (PathResult Sjt, PathResult Svc) FindShortestPaths(StationId from, StationId to)
    {
        var sjt = FindShortestPathInternal(from, to, fi => fi.SjtFare);
        var svc = FindShortestPathInternal(from, to, fi => fi.SvcFare);
        return (sjt, svc);
    }

    /// <summary>
    /// Dijkstra's algorithm with path reconstruction for a chosen fare metric.
    /// Runtime: O((V + E) log V) using a binary heap (PriorityQueue).
    /// </summary>
    /// <param name="from">Source station.</param>
    /// <param name="to">Destination station.</param>
    /// <param name="weightSelector">Selector choosing which fare (SJT/SVC) to minimize.</param>
    /// <returns>PathResult containing total cost and ordered list of stations.</returns>
    /// <exception cref="InvalidOperationException">If stations are missing or no path exists.</exception>
    private PathResult FindShortestPathInternal(StationId from, StationId to, Func<FareInfo, decimal> weightSelector)
    {
        // Validate endpoints exist in the graph.
        if (!Adjacency.ContainsKey(from))
            throw new InvalidOperationException(
                $"Station {from.Station} on line {from.TransitLine} does not exist in the graph.");

        if (!Adjacency.ContainsKey(to))
            throw new InvalidOperationException(
                $"Station {to.Station} on line {to.TransitLine} does not exist in the graph.");

        // Trivial case: same station.
        if (from.Equals(to))
            return new PathResult(0m, new List<StationId> { from });

        // Distance map initialized to +∞ (decimal.MaxValue).
        var distances = new Dictionary<StationId, decimal>(Adjacency.Count);
        // Predecessor map for path reconstruction.
        var prev = new Dictionary<StationId, StationId?>(Adjacency.Count);

        foreach (var v in Adjacency.Keys)
        {
            distances[v] = decimal.MaxValue;
            prev[v] = null;
        }
        distances[from] = 0m;

        // Min-priority queue keyed by current best distance.
        var pq = new PriorityQueue<StationId, decimal>();
        pq.Enqueue(from, 0m);

        // Track settled nodes (final shortest distances).
        var visited = new HashSet<StationId>();

        while (pq.TryDequeue(out var u, out var distU))
        {
            // Skip if we already finalized this node.
            if (!visited.Add(u))
                continue;

            // Early exit: destination popped with its final shortest distance.
            if (u.Equals(to))
            {
                var path = ReconstructPath(prev, from, to);
                return new PathResult(distU, path);
            }

            // Get adjacency list; continue if isolated.
            if (!Adjacency.TryGetValue(u, out var neighbors))
                continue;

            // Relax each outgoing edge (u -> v).
            foreach (var (v, fareInfo) in neighbors)
            {
                var weight = weightSelector(fareInfo);
                if (weight < 0) continue; // Defensive: ignore invalid negative weights.

                var alt = distU + weight;
                // If we found a strictly better path to v, record it.
                if (alt >= distances[v])
                    continue;

                distances[v] = alt;
                prev[v] = u;
                pq.Enqueue(v, alt);
            }
        }

        // If loop finishes without returning, no path was discovered.
        throw new InvalidOperationException($"No path found from {from} to {to}.");
    }

    /// <summary>
    /// Reconstructs the path from source to destination using the predecessor map.
    /// </summary>
    private static List<StationId> ReconstructPath(Dictionary<StationId, StationId?> prev, StationId from, StationId to)
    {
        var path = new List<StationId>();
        var current = to;

        // Walk backwards from destination until reaching source (or missing predecessor).
        while (!current.Equals(from))
        {
            path.Add(current);
            if (!prev.TryGetValue(current, out var p) || p is null)
                break; // No complete path; partial safety net (should not occur if called correctly).
            current = p;
        }

        path.Add(from);
        path.Reverse(); // Now ordered from source -> destination.
        return path;
    }
}