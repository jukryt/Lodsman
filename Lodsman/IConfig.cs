namespace Lodsman;

internal interface IConfig
{
    string KeenAddress { get; }
    string KeenUser { get; }
    string KeenPassword { get; }
    string KeenListName { get; }
    List<string> ProcessNames { get; }
    bool ClearBeforeExit { get; }
}
