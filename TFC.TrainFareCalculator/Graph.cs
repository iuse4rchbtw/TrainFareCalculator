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

    public FareInfo FindShortestPath(StationId from, StationId to)
    {
        var sjt = FindShortestPath(from, to, fi => fi.SjtFare);
        var svc = FindShortestPath(from, to, fi => fi.SvcFare);

        return new FareInfo(sjt, svc);
    }

    // Overload to choose fare type (e.g., fi => fi.SjtFare)
    // Dijkstra's algorithm
    private decimal FindShortestPath(StationId from, StationId to, Func<FareInfo, decimal> weightSelector)
    {
        if (!Adjacency.ContainsKey(from))
            throw new InvalidOperationException(
                $"Station {from.Station} on line {from.TransitLine} does not exist in the graph.");

        if (!Adjacency.ContainsKey(to))
            throw new InvalidOperationException(
                $"Station {to.Station} on line {to.TransitLine} does not exist in the graph.");

        if (from.Equals(to)) return 0m;

        // Dijkstra: min-priority queue by total cost
        var distances = new Dictionary<StationId, decimal>();
        foreach (var v in Adjacency.Keys)
            distances[v] = decimal.MaxValue;
        distances[from] = 0m;

        var pq = new PriorityQueue<StationId, decimal>();
        pq.Enqueue(from, 0m);

        var visited = new HashSet<StationId>();

        while (pq.TryDequeue(out var u, out var distU))
        {
            if (!visited.Add(u))
                continue;

            if (u.Equals(to))
                return distU;

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
                pq.Enqueue(v, alt);
            }
        }

        throw new InvalidOperationException($"No path found from {from} to {to}.");
    }
}