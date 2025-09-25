using System.Text.Json.Serialization;

namespace TFC.TrainFareCalculator;

internal record DirectoryInfo(List<string> MatrixPaths, string TransfersPath);

public record Transfer(TransferEndpoint From, TransferEndpoint To);

public record TransferEndpoint(string TransitLine, string Code);

public record Fares(List<List<int>> SingleJourneyTicket, List<List<int>> StoredValueCard);

public record Matrix(string TransitLine, Fares Fares)
{
    [JsonIgnore]
    public List<Station> Stations => LocalStations.Select(s => new Station(TransitLine, s.Code, s.Name)).ToList();

    [JsonInclude]
    [JsonPropertyName("stations")]
    private List<LocalStation> LocalStations { get; set; }

    private record LocalStation(string Code, string Name);
}

public record Station(string TransitLine, string Code, string Name)
{
    public override string ToString()
    {
        return $"({Code}) {TransitLine} {Name}";
    }
}
public record FareInfo(decimal StoredValueCard, decimal SingleJourneyTicket);