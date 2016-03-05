using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using GXTConvert.Exceptions;
using GXTConvert.FileFormat;

namespace GXTConvert.Conversion
{
    // For convenience sake, to bundle (most of) the essential texture information
    public class TextureBundle
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int PaletteIndex { get; private set; }
        public SceGxmTextureBaseFormat TextureBaseFormat { get; private set; }

        public PixelFormat PixelFormat { get; private set; }
        public byte[] PixelData { get; private set; }

        public TextureBundle(BinaryReader reader, SceGxtHeader header, SceGxtTextureInfo info)
        {
            reader.BaseStream.Seek(info.DataOffset, SeekOrigin.Begin);

            Width = info.GetWidth();
            Height = info.GetHeight();
            PaletteIndex = info.PaletteIndex;
            TextureBaseFormat = info.GetTextureBaseFormat();

            SceGxmTextureFormat textureFormat = info.GetTextureFormat();

            if (!PixelDataProviders.PixelFormatMap.ContainsKey(textureFormat) || !PixelDataProviders.ProviderFunctions.ContainsKey(textureFormat))
                throw new FormatNotImplementedException(textureFormat);

            PixelFormat = PixelDataProviders.PixelFormatMap[textureFormat];
            PixelData = PixelDataProviders.ProviderFunctions[textureFormat](reader, info);

            // TODO: is this right? PVRTC doesn't need this, but everything else does?
            if (TextureBaseFormat != SceGxmTextureBaseFormat.PVRT2BPP && TextureBaseFormat != SceGxmTextureBaseFormat.PVRT4BPP)
            {
                SceGxmTextureType textureType = info.GetTextureType();
                switch (textureType)
                {
                    case SceGxmTextureType.Linear:
                        // Nothing to be done!
                        break;

                    case SceGxmTextureType.Tiled:
                        // TODO: verify me!
                        PixelData = PostProcessing.UntileTexture(PixelData, Width, Height, PixelFormat);
                        break;

                    case SceGxmTextureType.Swizzled:
                    case SceGxmTextureType.Cube:
                        // TODO: is cube really the same as swizzled? seems that way from CS' *env* files...
                        PixelData = PostProcessing.UnswizzleTexture(PixelData, Width, Height, PixelFormat);
                        break;

                    default:
                        throw new TypeNotImplementedException(textureType);
                }
            }
        }

        public Bitmap CreateTexture(Color[] palette = null)
        {
            Bitmap texture = new Bitmap(Width, Height, PixelFormat);
            BitmapData bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadWrite, texture.PixelFormat);

            byte[] pixelsForBmp = new byte[bmpData.Height * bmpData.Stride];
            int bytesPerPixel = (Bitmap.GetPixelFormatSize(PixelFormat) / 8);
            for (int y = 0; y < bmpData.Height; y++)
                Buffer.BlockCopy(PixelData, y * bmpData.Width * bytesPerPixel, pixelsForBmp, y * bmpData.Stride, bmpData.Width * bytesPerPixel);

            Marshal.Copy(pixelsForBmp, 0, bmpData.Scan0, pixelsForBmp.Length);
            texture.UnlockBits(bmpData);

            if (palette != null)
            {
                ColorPalette texturePalette = texture.Palette;
                for (int j = 0; j < palette.Length; j++) texturePalette.Entries[j] = palette[j];
                texture.Palette = texturePalette;
            }

            return texture;
        }
    }
}
