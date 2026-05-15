using Lodsman.Context;

namespace Lodsman.CliRunner;

internal class AppExecutor(IContext context)
{
    public IContext Context => context;

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var app = new App(context);

        await app.RunAsync(cancellationToken);
        await app.ShutdownAsync();

        return 0;
    }
}
