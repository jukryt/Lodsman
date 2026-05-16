namespace Lodsman.Context;

internal interface IConfig
{
    bool IsService { get; }
    List<string> ProcessNames { get; }
    bool ClearBeforeExit { get; }
}
