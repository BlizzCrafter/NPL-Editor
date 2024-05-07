using System.Linq;

namespace NPLEditor.Data
{
    public static class AddContentDescriptor
    {
        public static string Name = "";
        public static string Extension = "";

        public static void MakeNumberless()
        {
            Name = new(Name.Where(c => !char.IsDigit(c)).ToArray());
        }

        public static void Reset()
        {
            Name = "";
            Extension = "";
        }
    }
}
