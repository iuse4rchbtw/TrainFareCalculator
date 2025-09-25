using System.Text.Json;
using System.Text.Json.Serialization;

namespace TFC.TrainFareCalculator;

public class Directory
{
    public List<Matrix> Matrices { get; set; }
    public List<Transfer> Transfers { get; set; }

    public static Directory Load(string filePath)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = File.ReadAllText(filePath);
        var directoryInfo = JsonSerializer.Deserialize<DirectoryInfo>(jsonString, options);
        ValidateDirectoryInfo(directoryInfo);

        // load matrices
        var transitLines = new List<Matrix>();

        foreach (var file in directoryInfo.MatrixPaths)
        {
            // deserialize json file
            var matrixJson = File.ReadAllText(file);
            var matrix = JsonSerializer.Deserialize<Matrix>(matrixJson, options);
            ValidateMatrix(file, matrix);

            transitLines.Add(matrix);
        }

        // load transfers
        var transfersJson = File.ReadAllText(directoryInfo.TransfersPath);
        var transfers = JsonSerializer.Deserialize<List<Transfer>>(transfersJson, options);

        if (transfers is null)
            throw new InvalidDataException($"Failed to deserialize transfers file: {directoryInfo.TransfersPath}");

        return new Directory { Matrices = transitLines, Transfers = transfers };
    }

    private static void ValidateMatrix(string path, Matrix? matrix)
    {
        if (matrix is null)
            throw new InvalidDataException($"Failed to deserialize matrix file: {path}");

        if (matrix.Stations is null || matrix.Fares is null)
            throw new InvalidDataException($"Incomplete matrix data in file: {path}");
    }

    private static void ValidateDirectoryInfo(DirectoryInfo? directoryInfo)
    {
        if (directoryInfo is null)
            throw new InvalidDataException("Failed to deserialize directory file.");

        if (directoryInfo.MatrixPaths is null || directoryInfo.MatrixPaths.Count == 0)
            throw new InvalidDataException("No matrices defined in directory file.");
    }
}
