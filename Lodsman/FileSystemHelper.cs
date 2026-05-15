using Lodsman.Main;

namespace Lodsman;

internal static class FileSystemHelper
{
    public static string GetAppDataFolder()
    {
        var programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(programDataFolder, App.Name);
    }

    public static string NormalizeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}
