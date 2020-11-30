using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MultiplayerHost
{
    public static class Extensions
    {
        public static ILogger WithMethodName(this ILogger logger, [CallerMemberName] string method = null)
        {
            var name = (method?.StartsWith('.') ?? false) ? method : '.' + method;
            Serilog.Context.LogContext.PushProperty("MethodName", name);
            return logger;
        }
    }
}
