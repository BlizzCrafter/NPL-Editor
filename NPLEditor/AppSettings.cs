using Serilog;
using System;
using System.IO;

namespace NPLEditor
{
    public static class AppSettings
    {
        public static readonly string Title = "NPL Editor";
        public static readonly string Author = "BlizzCrafter";
        public static readonly string Description = "The NPLEditor is a heavenly modernized and improved version of the MonoGame Pipeline Tool.";
        public static readonly string License = "The MIT License (MIT)";
        public static readonly string GitHubRepoURL = "https://github.com/BlizzCrafter/NPL-Editor";

        public static readonly string LocalContentPath = Path.Combine(AppContext.BaseDirectory, "Content");
        public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Title);
        public static readonly string LogPath = Path.Combine(AppDataPath, "logs");
        public static readonly string AllLogPath = Path.Combine(LogPath, "log.txt");
        public static readonly string ImportantLogPath = Path.Combine(LogPath, "important-log.txt");

        public static string NPLJsonFilePath = LocalContentPath;

        public static readonly bool ImGuiINI = false;

        public static void Init(string[] args)
        {
            // Create the app-data directory.
            Directory.CreateDirectory(AppDataPath);

            try
            {
                // The general log file should always regenerate.
                if (File.Exists(AllLogPath)) File.Delete(AllLogPath);
            }
            catch (IOException e)
            {
                // Handle the exception if the file is being used by another process.
                Log.Warning(e, $"Error deleting file: {AllLogPath}");
            }

            // Set the working directory.
#if DEBUG
            string projectDir = Directory.GetParent(LocalContentPath).Parent.Parent.FullName;
            string contentDir = Path.Combine(projectDir, "Content");
            Directory.SetCurrentDirectory(contentDir);

            NPLJsonFilePath = Path.Combine(contentDir, "Content.npl");
#else
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Content"));
            
            if (args != null && args.Length > 0)
            {
                // A .npl content file is always the first argument.
                NPLJsonFilePath = args[0];
            }
            else throw new ArgumentException("This app needs to have at least one launch argument with a path pointing to a .npl content file.");
#endif
        }
    }
}
