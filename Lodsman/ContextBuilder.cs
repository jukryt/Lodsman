using Lodsman.Router.Keenetic;

namespace Lodsman;

internal static class ContextBuilder
{
    public static async Task<IContext> BuildAsync(IConfig config)
    {
        return await KeeneticContext.BuildAsync(config);
    }
}
