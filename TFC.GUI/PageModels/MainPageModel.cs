using System.Windows.Input;
using Metalama.Patterns.Observability;
using TFC.TrainFareCalculator;
using Directory = TFC.TrainFareCalculator.Directory;

namespace TFC.GUI.PageModels;

[Observable]
public partial class MainPageModel
{
    public List<string> TransitLines { get; }
    
    private int _selectedFromTransitLineIdx = -1;
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
    
    private int _selectedToTransitLineIdx = -1;
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
    
    private readonly Directory _directory;
    private readonly Graph _graph;
    
    public decimal CalculatedFare { get; private set; }
    public bool IsDataComplete =>
        SelectedFromTransitLineIdx != -1 &&
        SelectedToTransitLineIdx != -1 &&
        SelectedFromStationIdx != -1 &&
        SelectedToStationIdx != -1;
    public bool IsCalculationComplete { get; set; }

    public MainPageModel()
    {
        // read data from directory
        _directory = Directory.Load("directory.json");
        _graph = GraphBuilder.Build(_directory);

        TransitLines = _directory.Matrices.Select(m => m.TransitLine).ToList();
    }
    
    public ICommand CalculateFareCommand => new Command(CalculateFare);
    
    private void CalculateFare()
    {
        if (!IsDataComplete)
        {
            CalculatedFare = 0;
            return;
        }

        var fromTl = _directory.Matrices[SelectedFromTransitLineIdx];
        var toTl = _directory.Matrices[SelectedToTransitLineIdx];
        var fromStation = fromTl.Stations[SelectedFromStationIdx];
        var toStation = toTl.Stations[SelectedToStationIdx];
        
        SelectedFromTransitLine = fromTl.TransitLine;
        SelectedFromStationName = fromStation.Name;
        SelectedToTransitLine = toTl.TransitLine;
        SelectedToStationName = toStation.Name;
        
        var fareInfo = _graph.FindShortestPaths(
            new Station(fromStation.TransitLine, fromStation.Code, fromStation.Name),
            new Station(toStation.TransitLine, toStation.Code, toStation.Name));
        
        CalculatedFare = IsStoredValueCard ? fareInfo.StoredValueCard.Total : fareInfo.SingleJourneyTicket.Total;
        IsCalculationComplete = true;
    }
}