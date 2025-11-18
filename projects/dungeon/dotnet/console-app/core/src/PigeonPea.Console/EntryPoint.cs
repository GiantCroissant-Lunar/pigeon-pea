using Serilog;
using Serilog.Events;
using PigeonPea.Console;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
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
