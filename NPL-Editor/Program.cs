using Serilog;
using System.IO;

Directory.CreateDirectory(NPLEditor.AppSettings.LogsPath);

if (File.Exists(NPLEditor.AppSettings.AllLogPath)) File.Delete(NPLEditor.AppSettings.AllLogPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(NPLEditor.AppSettings.AllLogPath,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
        rollOnFileSizeLimit: true)
    .WriteTo.File(NPLEditor.AppSettings.ImportantLogPath,
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
        rollOnFileSizeLimit: true)
    .WriteTo.Debug(
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .WriteTo.Console(
    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .CreateLogger();

Log.Information("--- INITIALIZE ---");

using var game = new NPLEditor.Main();
game.Run();