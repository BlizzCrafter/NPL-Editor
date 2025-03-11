using NPLEditor;
using NPLEditor.Data;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        // Logging the launch arguments.
        Log.Verbose($"Launch Arguments: {args}");

        // Initialize app settings (working dir, other directories, etc).
        AppSettings.Init(args);

        // Create the serilog logger.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(AppSettings.AllLogPath,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                rollOnFileSizeLimit: true)
            .WriteTo.File(AppSettings.ImportantLogPath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                rollOnFileSizeLimit: true)
            .WriteTo.Debug(
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
            .WriteTo.Console(
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
            .WriteTo.NPLEditorSink(
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();

        // Initialize the content builder.
        ContentBuilder.Init();

        // Log some pathes before building content or launching the app.
        // AppSettings and ContentBuilder should be initialized before logging.
        Log.Debug($"WorkingDir: {Directory.GetCurrentDirectory()}");
        Log.Debug($"IntermediateDir: {ContentBuilder.IntermediatePath}");
        Log.Debug($"OutputDir: {ContentBuilder.OutputPath}");
        Log.Debug($"LocalContentDir: {AppSettings.LocalContentPath}");

        // Only build the content or just launch the app.
        if (AppSettings.LaunchArguments.ContainsKey("build"))
        {
            try
            {
                await ContentBuilder.BuildContent();
            }
            catch (Exception e) { NPLLog.LogException(e, "ERROR", true); }
        }
        else RunApp();
    }

    private static void RunApp()
    {
        // Log that main initialize begins.
        NPLLog.LogInfoHeadline(FontAwesome.Flag, "INITIALIZE");

        // Ensure DPI-Awareness isn't lost for the dotnet tool.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            [DllImport("user32.dll")]
            static extern bool SetProcessDPIAware();

            SetProcessDPIAware();
        }
        using var app = new Main();
        app.Run();
    }
}
