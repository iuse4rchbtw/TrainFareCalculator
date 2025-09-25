namespace TFC.TrainFareCalculator;

public class Graph
{
    // Stored nodes (index -> key)
    private readonly List<Station> _nodes = [];
    // Reverse lookup (key -> index)
    private readonly Dictionary<Station, int> _indexByKey = [];
    // Adjacency list: index -> (neighborIndex -> FareInfo)
    private readonly List<Dictionary<int, FareInfo>> _adjacency = [];

    // Thread-safe if needed for parallel build (optional)
    private readonly object _sync = new();

    // Adds (or reuses) a node and returns its index.
    private int GetOrAddNode(Station id)
    {
        if (_indexByKey.TryGetValue(id, out var existing))
            return existing;

        lock (_sync)
        {
            if (_indexByKey.TryGetValue(id, out existing))
                return existing;

            var index = _nodes.Count;
            _nodes.Add(id);
            _indexByKey[id] = index;
            _adjacency.Add(new Dictionary<int, FareInfo>());
            return index;
        }
    }

    // Ensure node exists without returning index (optional convenience)
    public void EnsureNode(Station id) => GetOrAddNode(id);

    // Add or update an undirected edge.
    public void AddEdge(Station from, Station to,
                        FareInfo fareInfo)
    {
        var a = GetOrAddNode(from);
        var b = GetOrAddNode(to);

        _adjacency[a][b] = fareInfo;
        _adjacency[b][a] = fareInfo;
    }

    // Zero-fare transfer (bi-directional).
    public void AddTransfer(Station from, Station to)
        => AddEdge(from, to, new FareInfo(0, 0));

    public record PathResult(decimal Total, IReadOnlyList<Station> Path);

    // Compute independent SJT & SVC minimal paths.
    public (PathResult Sjt, PathResult Svc) FindShortestPaths(
        Station from,
        Station to)
    {
        var fromIdx = ResolveIndex(from);
        var toIdx   = ResolveIndex(to);

        var sjt = Dijkstra(fromIdx, toIdx, fi => fi.SingleJourneyTicket);
        var svc = Dijkstra(fromIdx, toIdx, fi => fi.StoredValueCard);
        return (sjt, svc);
    }

    private int ResolveIndex(Station id)
    {
        return _indexByKey.TryGetValue(id, out var idx)
            ? idx
            : throw new InvalidOperationException($"Station not found: {id}");
    }

    private PathResult Dijkstra(int source, int target, Func<FareInfo, decimal> weightSelector)
    {
        if (source == target)
        {
            var lone = _nodes[source];
            return new PathResult(0m, [lone]);
        }

        var n = _nodes.Count;
        var dist = new decimal[n];
        var prev = new int[n];
        var visited = new bool[n];

        for (var i = 0; i < n; i++)
        {
            dist[i] = decimal.MaxValue;
            prev[i] = -1;
        }
        dist[source] = 0m;

        var pq = new PriorityQueue<int, decimal>();
        pq.Enqueue(source, 0m);

        while (pq.TryDequeue(out var u, out var du))
        {
            if (visited[u]) continue;
            visited[u] = true;

            if (u == target)
                return new PathResult(du, ReconstructPath(prev, source, target));

            foreach (var (v, fareInfo) in _adjacency[u])
            {
                if (visited[v]) continue;
                var w = weightSelector(fareInfo);
                if (w < 0) continue; // Defensive
                var alt = du + w;
                if (alt >= dist[v]) continue;

                dist[v] = alt;
                prev[v] = u;
                pq.Enqueue(v, alt);
            }
        }

        throw new InvalidOperationException(
            $"No path found between {_nodes[source]} and {_nodes[target]}");
    }

    private IReadOnlyList<Station> ReconstructPath(int[] prev, int source, int target)
    {
        var stack = new Stack<Station>();
        var cur = target;
        while (cur != -1)
        {
            var k = _nodes[cur];
            stack.Push(k);
            if (cur == source) break;
            cur = prev[cur];
        }
        if (stack.Peek().TransitLine != _nodes[source].TransitLine || stack.Peek().Code != _nodes[source].Code)
            throw new InvalidOperationException("Path reconstruction failed.");
        return stack.ToList();
    }
}