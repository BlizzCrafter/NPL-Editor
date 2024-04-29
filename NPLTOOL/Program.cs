using Serilog;
using System;
using System.IO;

string logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

string mainLogPath = Path.Combine(logsPath, "log.txt");
string importantLogPath = Path.Combine(logsPath, "important.txt");

if (File.Exists(mainLogPath)) File.Delete(mainLogPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(mainLogPath,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
        rollOnFileSizeLimit: true)
    .WriteTo.File(importantLogPath,
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
        rollOnFileSizeLimit: true)
    .WriteTo.Debug(
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .CreateLogger();

Log.Information("--- INITIALIZE ---");

using var game = new NPLTOOL.Main();
game.Run();