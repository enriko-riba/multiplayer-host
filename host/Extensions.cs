using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MultiplayerHost
{
    /// <summary>
    /// Generic extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds the caller method name to log properties.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static ILogger WithMethodName(this ILogger logger, [CallerMemberName] string method = null)
        {
            var name = (method?.StartsWith('.') ?? false) ? method : '.' + method;
            Serilog.Context.LogContext.PushProperty("MethodName", name);
            return logger;
        }
    }
}
