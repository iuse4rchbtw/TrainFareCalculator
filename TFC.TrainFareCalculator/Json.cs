using System.Text.Json.Serialization;

namespace TFC.TrainFareCalculator;

internal class DirectoryInfo
{
    public List<string> MatrixPaths { get; set; }
    public string TransfersPath { get; set; }
}

public class Transfer
{
    public TransferEndpoint From { get; set; }
    public TransferEndpoint To { get; set; }
}

public class TransferEndpoint
{
    public string TransitLine { get; set; }
    public string Code { get; set; }
}

public class Fares
{
    [JsonPropertyName("sjt")]
    public List<List<int>> SingleJourneyTicket { get; set; }

    [JsonPropertyName("svc")]
    public List<List<int>> StoredValueCard { get; set; }
}

public class Matrix
{
    public string TransitLine { get; set; }

    [JsonIgnore]
    public List<Station> Stations => LocalStations.Select(s => new Station(TransitLine, s.Code, s.Name)).ToList();

    [JsonInclude]
    [JsonPropertyName("stations")]
    private List<LocalStation> LocalStations { get; set; }
    public Fares Fares { get; set; }

    private class LocalStation
    {
        public string Code { get; set; }
        public string Name { get; set; }

        public override string ToString() => $"{Code} - {Name}";
    }
}

public record Station(string TransitLine, string Code, string Name)
{
    public override string ToString()
    {
        return $"({Code}) {TransitLine} {Name}";
    }
}
public record FareInfo(decimal StoredValueCard, decimal SingleJourneyTicket);