using Lodsman.AddressSaver;
using Lodsman.Context;

namespace Lodsman.CliRunner
{
    internal class AppExecutor(IContext context)
    {
        public IContext Context => context;

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, context.Log, cancellationToken);
                var app = new App(context, addressSaverProcessor);

                await app.RunAsync(cancellationToken);
                await context.ShutdownAsync();
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                return ex.HResult;
            }

            return 0;
        }
    }
}
