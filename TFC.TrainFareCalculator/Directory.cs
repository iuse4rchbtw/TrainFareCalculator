using System.Text.Json;

namespace TFC.TrainFareCalculator;

public class Directory
{
    public required List<Matrix> Matrices { get; init; }
    public required List<Transfer> Transfers { get; init; }

    public static async Task<Directory> LoadAsync(string filePath, Func<string, Task<string>> fs)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonString = await fs(filePath);
        var directoryInfo = JsonSerializer.Deserialize<DirectoryInfo>(jsonString, options);
        ValidateDirectoryInfo(directoryInfo);

        // load matrices
        var transitLines = new List<Matrix>();

        foreach (var file in directoryInfo!.MatrixPaths)
        {
            // deserialize json file
            var matrixJson = await fs(file);
            var matrix = JsonSerializer.Deserialize<Matrix>(matrixJson, options);
            ValidateMatrix(file, matrix);

            transitLines.Add(matrix!);
        }

        // load transfers
        var transfersJson = await fs(directoryInfo.TransfersPath);
        var transfers = JsonSerializer.Deserialize<List<Transfer>>(transfersJson, options);

        return transfers is null
            ? throw new InvalidDataException($"Failed to deserialize transfers file: {directoryInfo.TransfersPath}")
            : new Directory { Matrices = transitLines, Transfers = transfers };
    }

    public static Directory Load(string filePath)
    {
        return LoadAsync(filePath, async path => await File.ReadAllTextAsync(path)).GetAwaiter().GetResult();
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