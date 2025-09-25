namespace TFC.GUI.Loaders;

public static class MauiAssetLoader
{
    public static async Task<string> LoadMauiAsset(string assetName)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
        using var reader = new StreamReader(stream);

        var text = await reader.ReadToEndAsync();
        return text;
    }
}