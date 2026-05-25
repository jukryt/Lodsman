using System.Runtime.ExceptionServices;

namespace Lodsman.Extension;

internal static class ExceptionExtension
{
    public static void Rethrow(this Exception ex)
    {
        var info = ExceptionDispatchInfo.Capture(ex);
        info.Throw();
    }
}
