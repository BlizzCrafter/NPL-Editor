using System;
using System.IO;

namespace NPLEditor
{
    public static class AppSettings
    {
        public static readonly string GitHubRepoURL = "https://github.com/BlizzCrafter/NPL-Editor";
        public static string LogsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        public static string AllLogPath = Path.Combine(LogsPath, "log.txt");
        public static string ImportantLogPath = Path.Combine(LogsPath, "important-log.txt");
        public static string BuildContentLogPath = Path.Combine(LogsPath, "build.txt");
    }
}
