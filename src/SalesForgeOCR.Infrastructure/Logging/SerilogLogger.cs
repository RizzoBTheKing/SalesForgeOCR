using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace SalesForgeOCR.Infrastructure.Logging
{
    public static class SerilogLogger
    {
        public static Logger CreateLogger(string? logFile = null)
        {
            logFile ??= "logs\\salesforgeocr-.log";
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
