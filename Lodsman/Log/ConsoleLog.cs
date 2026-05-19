namespace Lodsman.Log;

internal class ConsoleLog : BaseLog
{
    protected override void Write(string message)
    {
        Console.WriteLine(message);
    }
}
