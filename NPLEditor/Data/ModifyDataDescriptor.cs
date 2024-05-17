namespace NPLEditor.Data
{
    public static class ModifyDataDescriptor
    {
        public static string DataKey { get; private set; }
        public static string ItemKey { get; private set; }
        public static string ParamKey { get; private set; }
        public static dynamic Value { get; private set; }

        public static bool ForceWrite { get; set; }
        public static bool HasData { get; private set; }
        public static bool ParamModify { get; private set; }

        public static void Set(string dataKey, string itemKey, dynamic dataValue, string paramKey = "")
        {
            HasData = true;

            DataKey = dataKey;
            ItemKey = itemKey;
            ParamKey = paramKey;
            Value = dataValue;

            if (!string.IsNullOrEmpty(paramKey))
            {
                ParamModify = true;
            }
        }

        public static void Reset()
        {
            HasData = false;
            ParamModify = false;

            DataKey = string.Empty;
            ItemKey = string.Empty;
            ParamKey = string.Empty;
            Value = null;
        }
    }
}
