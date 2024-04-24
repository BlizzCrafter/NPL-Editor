using System.Numerics;

namespace NPLTOOL
{
    public static class Extensions
    {
        public static Microsoft.Xna.Framework.Color ToXNA(this Vector4 color)
        {
            return new Microsoft.Xna.Framework.Color(
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.X, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.Y, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.Z, 0f, 1f) * 255f),
                (byte)(Microsoft.Xna.Framework.MathHelper.Clamp(color.W, 0f, 1f) * 255f)
            );
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
    }
}
