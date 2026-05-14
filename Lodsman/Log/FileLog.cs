namespace Lodsman.Log;

internal class FileLog : BaseLog
{
    private readonly string _filePath;

    public FileLog(string filePath)
    {
        _filePath = filePath;
        Create(filePath);
    }

    protected override void Write(string message)
    {
        try
        {
            File.AppendAllLines(_filePath, [message]);
        }
        catch
        {
            // ignore
        }
    }

    private void Create(string filePath)
    {
        try
        {
            var folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath))
                Directory.CreateDirectory(folderPath);
            File.Create(filePath).Dispose();
        }
        catch
        {
            // ignore
        }
    }
}
