using NPLEditor;
using NPLEditor.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        // Create the logs directory.
        Directory.CreateDirectory(AppSettings.LogsPath);

        // The general log file should always regenerate.
        if (File.Exists(AppSettings.AllLogPath)) File.Delete(AppSettings.AllLogPath);

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

        // Parse command-line arguments
        var arguments = ParseArguments(args);
        if (arguments.ContainsKey("build"))
        {
#if DEBUG
            string projectDir = Directory.GetParent(AppSettings.LocalContentPath).Parent.Parent.FullName;
            string contentDir = Path.Combine(projectDir, "Content");
            Directory.SetCurrentDirectory(contentDir);
#else
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Content"));
#endif
            var nplJsonFilePath = args[1];
            try
            {
                string jsonString;
                using (var fs = new FileStream(nplJsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    jsonString = reader.ReadToEnd();
                }
                var jsonObject = JsonNode.Parse(jsonString);

                ContentBuilder.Init(jsonObject);

                await ContentBuilder.BuildContent();
            }
            catch (Exception e) { NPLLog.LogException(e, "ERROR", true); }
        }
        else RunGame();
    }

    private static void RunGame()
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
        using var game = new Main();
        game.Run();
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();
        foreach (var arg in args)
        {
            var splitArg = arg.Split('=');
            if (splitArg.Length == 2)
            {
                arguments[splitArg[0]] = splitArg[1];
            }
            else
            {
                arguments[splitArg[0]] = null;
            }
        }
        return arguments;
    }
}
