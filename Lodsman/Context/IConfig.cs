namespace Lodsman.Context;

internal interface IConfig
{
    List<string> ProcessNames { get; }
    bool ClearBeforeExit { get; }
}
