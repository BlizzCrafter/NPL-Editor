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
            return new Microsoft.Xna.Framework.Color(color.X * 255f, color.Y * 255f, color.Z * 255f, color.W * 255);
        }
    }
}
