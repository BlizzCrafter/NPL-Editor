using Microsoft.Xna.Framework.Content.Pipeline;
using Serilog;
using System;

namespace NPLEditor.Data
{
    public class NPLBuildLogger : ContentBuildLogger
    {
        public override void LogImportantMessage(string message, params object[] messageArgs)
        {
            NPLLog.LogWarningHeadline(FontAwesome.ExclamationCircle, message);
        }

        public override void LogMessage(string message, params object[] messageArgs)
        {
            Log.Debug(message);
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
            NPLLog.LogWarningHeadline(FontAwesome.ExclamationTriangle, message);
        }
    }
}
