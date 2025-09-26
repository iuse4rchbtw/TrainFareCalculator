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

    private int _selectedFromTransitLine = -1;

    private int _selectedToTransitLine = -1;
    
    public async Task InitializeAsync()
    {
        _directory = await Directory.LoadAsync("directory.json", MauiAssetLoader.LoadMauiAsset);
        _graph = GraphBuilder.Build(_directory);

        TransitLines = _directory.Matrices.Select(m => m.TransitLine).ToList();
    }

    public List<string> TransitLines { get; set; }

    public int SelectedFromTransitLine
    {
        get => _selectedFromTransitLine;
        set
        {
            _selectedFromTransitLine = value;
            SelectedFromStation = -1; // reset selected station when transit line changes
        }
    }

    public int SelectedToTransitLine
    {
        get => _selectedToTransitLine;
        set
        {
            _selectedToTransitLine = value;
            SelectedToStation = -1; // reset selected station when transit line changes
        }
    }

    public List<string> FromStations
    {
        get
        {
            if (SelectedFromTransitLine == -1)
                return [];

            return _directory.Matrices
                .First(m => m.TransitLine == _directory.Matrices[SelectedFromTransitLine].TransitLine)
                .Stations
                .Select(s => s.Name)
                .ToList();
        }
    }

    public int SelectedFromStation { get; set; } = -1;
    public List<string> ToStations
    {
        get
        {
            if (SelectedToTransitLine == -1)
                return [];

            return _directory.Matrices
                .First(m => m.TransitLine == _directory.Matrices[SelectedToTransitLine].TransitLine)
                .Stations
                .Select(s => s.Name)
                .ToList();
        }
    }

    public int SelectedToStation { get; set; } = -1;
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
        SelectedFromTransitLine != -1 &&
        SelectedToTransitLine != -1 &&
        SelectedFromStation != -1 &&
        SelectedToStation != -1;
    
    private Graph.Paths CalculateFare()
    {
        var fromTl = _directory.Matrices[SelectedFromTransitLine];
        var toTl = _directory.Matrices[SelectedToTransitLine];
        var fromStation = fromTl.Stations[SelectedFromStation];
        var toStation = toTl.Stations[SelectedToStation];

        var paths = _graph.FindShortestPaths(
            new Station(fromStation.TransitLine, fromStation.Code, fromStation.Name),
            new Station(toStation.TransitLine, toStation.Code, toStation.Name));

        return paths;
    }
}