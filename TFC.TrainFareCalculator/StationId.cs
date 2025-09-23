namespace TFC.TrainFareCalculator;

public record StationId(TransitLine TransitLine, string Station)
{
    public override string ToString()
    {
        return $"{TransitLine.GetDescription()} {Station}";
    }
}