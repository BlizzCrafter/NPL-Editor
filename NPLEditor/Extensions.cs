using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NPLEditor
{
    public static class Extensions
    {
        public static void ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey fromKey, TKey toKey)
        {
            TValue value = dictionary[fromKey];
            dictionary.Remove(fromKey);
            dictionary[toKey] = value;
        }

        public static Microsoft.Xna.Framework.Color ToXNA(this Vector4 color)
        {
            return new Microsoft.Xna.Framework.Color(
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.X, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.Y, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.Z, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.W, 0f, 1f) * 255f)
            );
        }

        public static string Parsable(this Microsoft.Xna.Framework.Color color)
        {
            return $"{color.R},{color.G},{color.B},{color.A}";
        }

        public static Vector4 ToVector4(this string value)
        {
            string[] rgbValues = value.Split(',');
            int red = int.Parse(rgbValues[0]);
            int green = int.Parse(rgbValues[1]);
            int blue = int.Parse(rgbValues[2]);
            int alpha = int.Parse(rgbValues[3]);
            return new Vector4(red / 255f, green / 255f, blue / 255f, alpha / 255);
        }

        public static bool ToBool(this string value)
        {
            return bool.Parse(value);
        }

        public static int ToInt(this string value)
        {
            return int.Parse(value);
        }

        public static double ToDouble(this string value)
        {
            return double.Parse(value);
        }

        public static float ToFloat(this string value)
        {
            return float.Parse(value);
        }

        public static string Numberless(this string value)
        {
            return new(value.Where(c => !char.IsDigit(c)).ToArray());
        }

        public static void NumberlessRef(ref string value)
        {
            value = value.Numberless();
        }
    }
}
