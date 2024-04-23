using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System;
using System.Numerics;

namespace NPLTOOL.Parameter
{
    public enum ParameterKey
    {
        ColorKeyColor,
        ColorKeyEnabled,
        GenerateMipmaps,
        PremultiplyAlpha,
        ResizeToPowerOfTwo,
        MakeSquare,
        TextureFormat
    }

    public class TextureProcessorParameter : IParameterProcessor
    {
        public Vector4 ColorKeyColor;
        public bool ColorKeyEnabled;
        public bool GenerateMipmaps;
        public bool PremultiplyAlpha;
        public bool ResizeToPowerOfTwo;
        public bool MakeSquare;
        public TextureProcessorOutputFormat TextureFormat;

        public static string ProcessorType => "TextureProcessor";

        public void SetValue(string key, object value)
        {
            if (key == ParameterKey.ColorKeyColor.ToString()) ColorKeyColor = value.ToString().Parse();
            else if (key == ParameterKey.ColorKeyEnabled.ToString()) ColorKeyEnabled = bool.Parse(value.ToString());
            else if (key == ParameterKey.GenerateMipmaps.ToString()) GenerateMipmaps = bool.Parse(value.ToString());
            else if (key == ParameterKey.PremultiplyAlpha.ToString()) PremultiplyAlpha = bool.Parse(value.ToString());
            else if (key == ParameterKey.ResizeToPowerOfTwo.ToString()) ResizeToPowerOfTwo = bool.Parse(value.ToString());
            else if (key == ParameterKey.MakeSquare.ToString()) MakeSquare = bool.Parse(value.ToString());
            else if (key == ParameterKey.TextureFormat.ToString()) TextureFormat = Enum.Parse<TextureProcessorOutputFormat>(value.ToString());
        }
    }
}
