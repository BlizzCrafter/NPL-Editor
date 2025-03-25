using System;
using System.IO;

namespace NPLEditor
{
    public static class AppSettings
    {
        public static readonly string Title = "NPL Editor";
        public static readonly string Author = "BlizzCrafter";
        public static readonly string Description = "A graphical editor for '.npl' files used together with 'Nopipeline' to produce '.mgcb' files for MonoGame projects.";
        public static readonly string License = "The MIT License (MIT)";
        public static readonly string GitHubRepoURL = "https://github.com/BlizzCrafter/NPL-Editor";

        public static readonly string LocalContentPath = Path.Combine(AppContext.BaseDirectory, "Content");
        public static readonly string LogsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        public static readonly string AllLogPath = Path.Combine(LogsPath, "log.txt");
        public static readonly string ImportantLogPath = Path.Combine(LogsPath, "important-log.txt");
        public static readonly string BuildContentLogPath = Path.Combine(LogsPath, "build.txt");

        public static string NPLJsonFilePath = LocalContentPath;

        public static readonly bool ImGuiINI = false;

        public static void Init(string[] args)
        {
            // Create the logs directory.
            Directory.CreateDirectory(LogsPath);

            try
            {
                // The general log file should always regenerate.
                if (File.Exists(AllLogPath)) File.Delete(AllLogPath);
            }
            catch { }

            // Set the working directory.
#if DEBUG
            string projectDir = Directory.GetParent(LocalContentPath).Parent.Parent.FullName;
            string contentDir = Path.Combine(projectDir, "Content");
            Directory.SetCurrentDirectory(contentDir);

            NPLJsonFilePath = Path.Combine(contentDir, "Content.npl");
#else
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Content"));
            
            if (args.Length >= 0)
            {
                // A .npl content file is always the first argument.
                NPLJsonFilePath = args[0];
            }
            else throw new ArgumentException("This app needs to have at least one launch argument with a path pointing to a .npl content file.");
#endif
        }
    }
}
