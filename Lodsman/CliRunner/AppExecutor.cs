using Lodsman.AddressSaver;
using Lodsman.Context;

namespace Lodsman.CliRunner;

internal class AppExecutor(IContext context)
{
    public IContext Context => context;

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, context.Log, cancellationToken);
        var app = new App(context, addressSaverProcessor);

        await app.RunAsync(cancellationToken);
        await context.ShutdownAsync();

        return 0;
    }
}
