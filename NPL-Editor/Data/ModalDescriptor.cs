using NPLEditor.Enums;
using System.IO;

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
