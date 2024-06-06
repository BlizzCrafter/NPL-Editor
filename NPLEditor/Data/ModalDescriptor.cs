using System;
using System.IO;
using System.Reflection;
using NPLEditor.Enums;

namespace NPLEditor.Data
{
    public static class ModalDescriptor
    {
        public static string Title { get; private set; } = "";
        public static string Message { get; private set; } = "";
        public static bool IsOpen { get; private set; } = false;
        public static MessageType MessageType { get; private set; }

        public static void SetFileNotFound(string filePath, string message)
        {
            string shortMessage = $"'{Path.GetFileName(filePath)}' not found.";
            string longMessage = message;
            Set(MessageType.FileNotFound, $"{shortMessage}\n\n{longMessage}");
        }

        public static void SetAbout()
        {
            var appVersion = $"v.{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}";
            string shortMessage = $"{AppSettings.Title} {appVersion}\nCopyright {FontAwesome.Copyright} {DateTime.Now.Year} {AppSettings.Author}\n{AppSettings.License}";
            string longMessage = $"{AppSettings.Description}\n\nVisit the GitHub page for further help & extended license information.";
            Set(MessageType.About, $"{shortMessage}\n\n{longMessage}");
        }

        public static void Set(MessageType messageType, string message)
        {
            switch (messageType)
            {
                case MessageType.FileNotFound:
                    {
                        Title = $"{FontAwesome.ExclamationTriangle} error";
                        break;
                    }
                case MessageType.AddContent:
                    {
                        Title = $"{FontAwesome.FolderPlus} add content";
                        break;
                    }
                case MessageType.EditContent:
                    {
                        Title = $"{FontAwesome.Edit} edit content";
                        break;
                    }
                case MessageType.About:
                    {
                        Title = $"{FontAwesome.ListAlt} about";
                        break;
                    }
                default:
                    {
                        Title = $"{FontAwesome.ExclamationCircle} info";
                        break;
                    }
            }
            MessageType = messageType;
            Set(Title, message);
        }

        public static void Set(string title, string message)
        {
            Title = title;
            Message = message;
            IsOpen = true;
        }

        public static void Reset()
        {
            Title = "";
            Message = "";
            IsOpen = false;
        }
    }
}
