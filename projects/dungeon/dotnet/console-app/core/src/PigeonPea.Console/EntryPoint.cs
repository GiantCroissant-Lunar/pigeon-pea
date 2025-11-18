using System;
using System.IO;
using Serilog;
using Serilog.Events;
using PigeonPea.Console;

var baseDir = AppContext.BaseDirectory;
var logsDir = Path.Combine(baseDir, "logs");
Directory.CreateDirectory(logsDir);
var logFilePath = Path.Combine(logsDir, "console-serilog.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting PigeonPea.Console with args: {Args}", args);
    return GameEntrypoint.Run(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception in PigeonPea.Console");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
