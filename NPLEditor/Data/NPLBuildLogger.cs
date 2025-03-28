using Microsoft.Xna.Framework.Content.Pipeline;
using Serilog;

namespace NPLEditor.Data
{
    public class NPLBuildLogger : ContentBuildLogger
    {
        public override void LogImportantMessage(string message, params object[] messageArgs)
        {
            NPLLog.LogWarningHeadline(FontAwesome.ExclamationCircle, string.Format(message, messageArgs));
        }

        public override void LogMessage(string message, params object[] messageArgs)
        {
            Log.Information(string.Format(message, messageArgs));
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
            NPLLog.LogWarningHeadline(FontAwesome.ExclamationTriangle, string.Format(message, messageArgs));
        }
    }
}
