using System;
using Serilog;

namespace NPLEditor.Data
{
    public static class NPLLog
    {
        public static void LogInfoHeadline(string icon, string message)
        {
            Log.Information("");
            Log.Information($"{icon} {message}");
            Log.Information("");
        }

        public static void LogErrorHeadline(string icon, string message)
        {
            Log.Error("");
            Log.Error($"{icon} {message}");
            Log.Error("");
        }

        public static void LogException(Exception e, string title = "ERROR", bool throwException = false)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                LogErrorHeadline(FontAwesome.HeartBroken, $"{title}: {e.Message}");
            }
            else
            {
                Log.Error("");
                Log.Error($"{FontAwesome.HeartBroken} {title}: {Environment.NewLine}");
                Log.Error(e.ToString());
                Log.Error("");
            }

            if (throwException) throw new Exception(e.ToString());
        }
    }
}
