using Lodsman.Log;
using Lodsman.Router.Keenetic;

namespace Lodsman;

internal static class ContextBuilder
{
    public static async Task<IContext> BuildAsync(IConfig config, ILog log)
    {
        return await KeeneticContext.BuildAsync(config, log);
    }
}
