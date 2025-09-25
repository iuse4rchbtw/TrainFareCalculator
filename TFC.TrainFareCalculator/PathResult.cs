namespace TFC.TrainFareCalculator;

public record PathResult(decimal Total, IReadOnlyList<StationId> Path);