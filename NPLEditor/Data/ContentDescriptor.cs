using System.Linq;

namespace NPLEditor.Data
{
    public static class ContentDescriptor
    {
        public static string Name = "";
        public static string Path = "";
        public static string Category = "";
        public static string ErrorMessage { get; private set; }
        public static bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public static void Error(string message)
        {
            ErrorMessage = message;
        }

        public static void MakeNumberless()
        {
            Name = new(Name.Where(c => !char.IsDigit(c)).ToArray());
        }

        public static void Reset()
        {
            Name = "";
            Path = "";
            Category = "";
            ErrorMessage = "";
        }
    }
}
