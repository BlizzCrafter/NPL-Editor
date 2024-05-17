using System.IO;
using System.Runtime.InteropServices;
using Serilog;

// Ensure DPI-Awareness isn't lost for the dotnet tool.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    SetProcessDPIAware();
}

// Create the logs directory.
Directory.CreateDirectory(NPLEditor.AppSettings.LogsPath);

// The general log file should always regenerate.
if (File.Exists(NPLEditor.AppSettings.AllLogPath)) File.Delete(NPLEditor.AppSettings.AllLogPath);

// Create the serilog logger.
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

// Log that main initialize begins.
Log.Information("--- INITIALIZE ---");

// Main initialize.
using var game = new NPLEditor.Main();
game.Run();