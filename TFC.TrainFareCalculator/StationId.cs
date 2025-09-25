namespace TFC.TrainFareCalculator;

/// <summary>
/// Unique identifier for a station on a specific transit line.
/// </summary>
/// <param name="TransitLine">The transit line (e.g. LRT-1, MRT-3) in which the station belongs.</param>
/// <param name="Station">The name of the station.</param>
public record StationId(TransitLine TransitLine, string Station)
{
    public override string ToString()
    {
        // E.g. "MRT-3 Taft Avenue"
        return $"{TransitLine.GetDescription()} {Station}";
    }
}