using NPLEditor;
using NPLEditor.Common;
using NPLEditor.Data;
using NPLEditor.Enums;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class Program
{
    internal static Dictionary<LaunchParameter, string> _launchParameter;

    public static async Task Main(string[] args)
    {
        // Parsing launch arguments.
        _launchParameter = ParseArguments(args);

        var logLevel = LogEventLevel.Verbose;
        if (_launchParameter.ContainsKey(LaunchParameter.Verbosity))
        {
            if (string.Equals("Silent", _launchParameter[LaunchParameter.Verbosity], StringComparison.OrdinalIgnoreCase))
            {
                // Since serilog has no real silent mode we are just setting the log level to the highest one,
                // which is kinda a "silent" mode, because fatal errors will likely be thrown anyway.
                logLevel = LogEventLevel.Fatal;
            }
            else if (Enum.TryParse<LogEventLevel>(_launchParameter[LaunchParameter.Verbosity], true, out var verbosity))
            {
                logLevel = verbosity;
            }
        }

        // Create the serilog logger.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(logLevel))
            .WriteTo.File(AppSettings.AllLogPath,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                rollOnFileSizeLimit: true)
            .WriteTo.File(AppSettings.ImportantLogPath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollOnFileSizeLimit: true)
            .WriteTo.Debug(
                restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.Console(
            restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.NPLEditorSink(
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Debug("Main Running & Logger Initilized");
        Log.Debug("Launch Arguments: {0}", args);

        // Initialize app settings (working dir, other directories, etc).
        AppSettings.Init(args);

        // Initialize the ContentBuilder (reading the Content.npl file, etc).
        ContentBuilder.Init();

        // Log some pathes before building content or launching the app.
        // AppSettings and ContentBuilder should be initialized before logging.
        Log.Debug($"WorkingDir: {Directory.GetCurrentDirectory()}");
        Log.Debug($"IntermediateDir: {ContentBuilder.IntermediatePath}");
        Log.Debug($"OutputDir: {ContentBuilder.OutputPath}");
        Log.Debug($"LocalContentDir: {AppSettings.LocalContentPath}");

        // Only build the content or just launch the app.
        if (_launchParameter.ContainsKey(LaunchParameter.Build))
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
        NPLLog.LogInfoHeadline(FontAwesome.Flag, "INITIALIZE APP");

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

    private static Dictionary<LaunchParameter, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<LaunchParameter, string>(new LaunchParameterComparer());
        foreach (var arg in args)
        {
            var splitArg = arg.Split('=');

            if (Enum.TryParse<LaunchParameter>(splitArg[0], true, out var parameter))
            {
                if (splitArg.Length == 2)
                {
                    arguments[parameter] = splitArg[1];
                }
                else
                {
                    arguments[parameter] = null;
                }
            }
        }
        return arguments;
    }
}
