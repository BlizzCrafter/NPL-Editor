using System.Linq;

namespace NPLEditor.Data
{
    public static class ContentDescriptor
    {
        public static string Name = "";
        public static string Extension = "";
        public static string Category = "";

        public static void MakeNumberless()
        {
            Name = new(Name.Where(c => !char.IsDigit(c)).ToArray());
        }

        public static void Reset()
        {
            Name = "";
            Extension = "";
            Category = "";
        }
    }
}
