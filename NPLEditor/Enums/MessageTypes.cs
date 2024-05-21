using System;

namespace NPLEditor.Enums
{
    [Flags]
    public enum MessageType
    {
        Default = 0,
        AddContent = 1,
        EditContent = 2,
        FileNotFound = 4,
        About = 8,

        Cancel = AddContent | EditContent,
        Delete = EditContent
    }
}
