namespace TFC.TrainFareCalculator;

public class Graph
{
    private Dictionary<StationId, Dictionary<StationId, FareInfo>>
        Adjacency { get; } = [];

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

        Adjacency[to][from] = new FareInfo(0, 0);
        Adjacency[from][to] = new FareInfo(0, 0);
    }

    // Back-compat: returns only the totals, without the path.
    public FareInfo FindShortestPath(StationId from, StationId to)
    {
        var sjt = FindShortestPathInternal(from, to, fi => fi.SjtFare);
        var svc = FindShortestPathInternal(from, to, fi => fi.SvcFare);

        return new FareInfo(sjt.Total, svc.Total);
    }

    // New: returns both SJT and SVC paths and totals.
    public (PathResult Sjt, PathResult Svc) FindShortestPathsWithPath(StationId from, StationId to)
    {
        var sjt = FindShortestPathInternal(from, to, fi => fi.SjtFare);
        var svc = FindShortestPathInternal(from, to, fi => fi.SvcFare);
        return (sjt, svc);
    }


    // Dijkstra with path reconstruction for a chosen fare metric
    private PathResult FindShortestPathInternal(StationId from, StationId to, Func<FareInfo, decimal> weightSelector)
    {
        if (!Adjacency.ContainsKey(from))
            throw new InvalidOperationException(
                $"Station {from.Station} on line {from.TransitLine} does not exist in the graph.");

        if (!Adjacency.ContainsKey(to))
            throw new InvalidOperationException(
                $"Station {to.Station} on line {to.TransitLine} does not exist in the graph.");

        if (from.Equals(to))
            return new PathResult(0m, new List<StationId> { from });

        var distances = new Dictionary<StationId, decimal>(Adjacency.Count);
        var prev = new Dictionary<StationId, StationId?>(Adjacency.Count);

        foreach (var v in Adjacency.Keys)
        {
            distances[v] = decimal.MaxValue;
            prev[v] = null;
        }
        distances[from] = 0m;

        var pq = new PriorityQueue<StationId, decimal>();
        pq.Enqueue(from, 0m);

        var visited = new HashSet<StationId>();

        while (pq.TryDequeue(out var u, out var distU))
        {
            if (!visited.Add(u))
                continue;

            if (u.Equals(to))
            {
                var path = ReconstructPath(prev, from, to);
                return new PathResult(distU, path);
            }

            if (!Adjacency.TryGetValue(u, out var neighbors))
                continue;

            foreach (var (v, fareInfo) in neighbors)
            {
                var weight = weightSelector(fareInfo);
                if (weight < 0) continue; // defensive; fares should be non-negative

                var alt = distU + weight;
                if (alt >= distances[v])
                    continue;

                distances[v] = alt;
                prev[v] = u;
                pq.Enqueue(v, alt);
            }
        }

        throw new InvalidOperationException($"No path found from {from} to {to}.");
    }

    private static List<StationId> ReconstructPath(Dictionary<StationId, StationId?> prev, StationId from, StationId to)
    {
        var path = new List<StationId>();
        var current = to;

        // Walk backwards from destination to source
        while (!current.Equals(from))
        {
            path.Add(current);
            if (!prev.TryGetValue(current, out var p) || p is null)
                break; // no path
            current = p;
        }

        path.Add(from);
        path.Reverse();
        return path;
    }
}