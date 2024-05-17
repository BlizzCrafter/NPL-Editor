using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using System.Text;

namespace NPLEditor.Data
{
    public static class NPLEditorSinkExtensions
    {
        public static LoggerConfiguration NPLEditorSink(this LoggerSinkConfiguration loggerConfiguration, IFormatProvider formatProvider = null, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
        {
            return loggerConfiguration.Sink(new NPLEditorSink(formatProvider), restrictedToMinimumLevel);
        }
    }

    public class NPLEditorSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        public static StringBuilder Output;

        public NPLEditorSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
            Output = new StringBuilder();
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider);
            Output.AppendLine(DateTimeOffset.Now.ToString("HH:mm:ss") + " " + message);

            Main.ScrollLogToBottom = true;
        }
    }
}
