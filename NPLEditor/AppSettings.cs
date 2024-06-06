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

        public static readonly bool ImGuiINI = false;
    }
}
