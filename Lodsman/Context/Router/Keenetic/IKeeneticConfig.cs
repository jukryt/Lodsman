namespace Lodsman.Context.Router.Keenetic
{
    internal interface IKeeneticConfig : IConfig
    {
        string Address { get; }
        string User { get; }
        string Password { get; }
        string ListName { get; }
    }
}
