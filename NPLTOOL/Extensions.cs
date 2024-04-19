using System.Numerics;

namespace NPLTOOL
{
    public static class Extensions
    {
        public static Vector4 ToSystemVector4(this Microsoft.Xna.Framework.Color color)
        {
            return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
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

        public static Vector4 Parse(this string colorString)
        {
            string[] rgbValues = colorString.Split(',');
            int red = int.Parse(rgbValues[0]);
            int green = int.Parse(rgbValues[1]);
            int blue = int.Parse(rgbValues[2]);
            int alpha = int.Parse(rgbValues[3]);
            return new Vector4(red / 255f, green / 255f, blue / 255f, alpha / 255);
        }
    }
}
