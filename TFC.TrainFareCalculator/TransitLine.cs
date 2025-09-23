using System.ComponentModel;
using System.Reflection;

namespace TFC.TrainFareCalculator;

public enum TransitLine
{
    [Description("LRT-1")]
    GreenLine,  // LRT-1

    [Description("LRT-2")]
    PurpleLine, // LRT-2

    [Description("MRT-3")]
    YellowLine, // MRT-3
}
