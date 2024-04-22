using System.Numerics;

namespace NPLTOOL.Parameter
{
    public enum TextureFormat
    {        
        Color, 
        DxtCompressed, 
        NoChange, 
        Compressed,
        Color16Bit, 
        Etc1Compressed, 
        PvrCompressed, 
        AtcCompressed
    }

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

    public class TextureProcessorParameter
    {
        public Vector4 ColorKeyColor;
        public bool ColorKeyEnabled;
        public bool GenerateMipmaps;
        public bool PremultiplyAlpha;
        public bool ResizeToPowerOfTwo;
        public bool MakeSquare;
        public string TextureFormat;

        public void SetParameter(ParameterKey key, object value)
        {
            if (key == ParameterKey.ColorKeyColor) ColorKeyColor = value.ToString().Parse();
            else if (key == ParameterKey.ColorKeyEnabled) ColorKeyEnabled = bool.Parse(value.ToString());
            else if (key == ParameterKey.GenerateMipmaps) GenerateMipmaps = bool.Parse(value.ToString());
            else if (key == ParameterKey.PremultiplyAlpha) PremultiplyAlpha = bool.Parse(value.ToString());
            else if (key == ParameterKey.ResizeToPowerOfTwo) ResizeToPowerOfTwo = bool.Parse(value.ToString());
            else if (key == ParameterKey.MakeSquare) MakeSquare = bool.Parse(value.ToString());
            else if (key == ParameterKey.TextureFormat) TextureFormat = value.ToString();
        }
    }
}
