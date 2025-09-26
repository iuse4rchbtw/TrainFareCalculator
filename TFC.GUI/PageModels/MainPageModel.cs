using System.Windows.Input;
using Metalama.Patterns.Observability;
using TFC.GUI.Loaders;
using TFC.TrainFareCalculator;
using Directory = TFC.TrainFareCalculator.Directory;

namespace TFC.GUI.PageModels;

[Observable]
public partial class MainPageModel
{
    private Directory _directory;
    private Graph _graph;

    private int _selectedFromTransitLineIdx = -1;

    private int _selectedToTransitLineIdx = -1;
    
    public async Task InitializeAsync()
    {
        _directory = await Directory.LoadAsync("directory.json", MauiAssetLoader.LoadMauiAsset);
        _graph = GraphBuilder.Build(_directory);

        TransitLines = _directory.Matrices.Select(m => m.TransitLine).ToList();
    }

    public List<string> TransitLines { get; set; }

    public int SelectedFromTransitLineIdx
    {
        get => _selectedFromTransitLineIdx;
        set
        {
            _selectedFromTransitLineIdx = value;
            SelectedFromStationIdx = -1; // reset selected station when transit line changes
        }
    }

    public string SelectedFromTransitLine { get; set; } = "";

    public int SelectedToTransitLineIdx
    {
        get => _selectedToTransitLineIdx;
        set
        {
            _selectedToTransitLineIdx = value;
            SelectedToStationIdx = -1; // reset selected station when transit line changes
        }
    }

    public string SelectedToTransitLine { get; set; } = "";

    public List<string> FromStations
    {
        get
        {
            if (SelectedFromTransitLineIdx == -1)
                return [];

            return _directory.Matrices
                .First(m => m.TransitLine == _directory.Matrices[SelectedFromTransitLineIdx].TransitLine)
                .Stations
                .Select(s => s.Name)
                .ToList();
        }
    }

    public int SelectedFromStationIdx { get; set; } = -1;
    public string SelectedFromStationName { get; set; } = "";

    public List<string> ToStations
    {
        get
        {
            if (SelectedToTransitLineIdx == -1)
                return [];

            return _directory.Matrices
                .First(m => m.TransitLine == _directory.Matrices[SelectedToTransitLineIdx].TransitLine)
                .Stations
                .Select(s => s.Name)
                .ToList();
        }
    }

    public int SelectedToStationIdx { get; set; } = -1;
    public string SelectedToStationName { get; set; } = "";

    public bool IsStoredValueCard { get; set; } = true;
    public bool IsDiscounted { get; set; }

    public decimal CalculatedFare
    {
        get
        {
            if (!IsDataComplete)
                return 0m;
            
            var paths = CalculateFare();
            var calcFare = IsStoredValueCard ? paths.StoredValueCard.Total : paths.SingleJourneyTicket.Total;
            
            return IsDiscounted ? calcFare / 2 : calcFare; // apply 50% discount if applicable
        }
    }

    public List<Graph.PathComponent> CalculatedPath
    {
        get
        {
            if (!IsDataComplete)
                return [];
            
            var paths = CalculateFare();
            var calcPath = IsStoredValueCard 
                ? paths.StoredValueCard.Path 
                : paths.SingleJourneyTicket.Path;

            return IsDiscounted
                ? calcPath.Select(p => p with { Fare = p.Fare / 2 }).ToList()
                : calcPath.ToList();
        }
    }

    public bool IsDataComplete =>
        SelectedFromTransitLineIdx != -1 &&
        SelectedToTransitLineIdx != -1 &&
        SelectedFromStationIdx != -1 &&
        SelectedToStationIdx != -1;
    
    private Graph.Paths CalculateFare()
    {
        var fromTl = _directory.Matrices[SelectedFromTransitLineIdx];
        var toTl = _directory.Matrices[SelectedToTransitLineIdx];
        var fromStation = fromTl.Stations[SelectedFromStationIdx];
        var toStation = toTl.Stations[SelectedToStationIdx];

        SelectedFromTransitLine = fromTl.TransitLine;
        SelectedFromStationName = fromStation.Name;
        SelectedToTransitLine = toTl.TransitLine;
        SelectedToStationName = toStation.Name;

        var paths = _graph.FindShortestPaths(
            new Station(fromStation.TransitLine, fromStation.Code, fromStation.Name),
            new Station(toStation.TransitLine, toStation.Code, toStation.Name));

        return paths;
    }
}