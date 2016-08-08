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
        public int RawLineSize { get; private set; }
        public SceGxmTextureFormat TextureFormat { get; private set; }

        public PixelFormat PixelFormat { get; private set; }
        public byte[] PixelData { get; private set; }
        public int RoundedWidth { get; private set; }
        public int RoundedHeight { get; private set; }

        public TextureBundle(BinaryReader reader, SceGxtHeader header, SceGxtTextureInfo info)
        {
            reader.BaseStream.Seek(info.DataOffset, SeekOrigin.Begin);

            Width = info.GetWidth();
            Height = info.GetHeight();
            PaletteIndex = info.PaletteIndex;
            RawLineSize = (int)(info.DataSize / info.GetHeightRounded());
            TextureFormat = info.GetTextureFormat();

            RoundedWidth = info.GetWidthRounded();
            RoundedHeight = info.GetHeightRounded();

            if (!PixelDataProviders.PixelFormatMap.ContainsKey(TextureFormat) || !PixelDataProviders.ProviderFunctions.ContainsKey(TextureFormat))
                throw new FormatNotImplementedException(TextureFormat);

            PixelFormat = PixelDataProviders.PixelFormatMap[TextureFormat];
            PixelData = PixelDataProviders.ProviderFunctions[TextureFormat](reader, info);

            SceGxmTextureBaseFormat textureBaseFormat = info.GetTextureBaseFormat();

            // TODO: is this right? PVRTC/PVRTC2 doesn't need this, but everything else does?
            if (textureBaseFormat != SceGxmTextureBaseFormat.PVRT2BPP && textureBaseFormat != SceGxmTextureBaseFormat.PVRT4BPP &&
                textureBaseFormat != SceGxmTextureBaseFormat.PVRTII2BPP && textureBaseFormat != SceGxmTextureBaseFormat.PVRTII4BPP)
            {
                SceGxmTextureType textureType = info.GetTextureType();
                switch (textureType)
                {
                    case SceGxmTextureType.Linear:
                        // Nothing to be done!
                        break;

                    case SceGxmTextureType.Tiled:
                        // TODO: verify me!
                        PixelData = PostProcessing.UntileTexture(PixelData, info.GetWidthRounded(), info.GetHeightRounded(), PixelFormat);
                        break;

                    case SceGxmTextureType.Swizzled:
                    case SceGxmTextureType.Cube:
                        // TODO: is cube really the same as swizzled? seems that way from CS' *env* files...
                        PixelData = PostProcessing.UnswizzleTexture(PixelData, info.GetWidthRounded(), info.GetHeightRounded(), PixelFormat);
                        break;

                    case (SceGxmTextureType)0xA0000000:
                        // TODO: mehhhhh
                        PixelData = PostProcessing.UnswizzleTexture(PixelData, info.GetWidthRounded(), info.GetHeightRounded(), PixelFormat);
                        break;

                    default:
                        throw new TypeNotImplementedException(textureType);
                }
            }
        }

        public Bitmap CreateTexture(Color[] palette = null)
        {
            Bitmap texture = new Bitmap(RoundedWidth, RoundedHeight, PixelFormat);
            BitmapData bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadWrite, texture.PixelFormat);

            byte[] pixelsForBmp = new byte[bmpData.Height * bmpData.Stride];
            int bytesPerPixel = (Bitmap.GetPixelFormatSize(PixelFormat) / 8);

            for (int y = 0; y < bmpData.Height; y++)
            {
                // TODO: verify this ([RawLineSize] vs the old [bmpData.Width * bytesPerPixel]) doesn't cause issues with other indexed files!
                int srcOffset = y * (PixelFormat == PixelFormat.Format4bppIndexed || PixelFormat == PixelFormat.Format8bppIndexed ? RawLineSize : bmpData.Width * bytesPerPixel);
                int dstOffset = y * bmpData.Stride;
                if (srcOffset >= PixelData.Length || dstOffset >= pixelsForBmp.Length) continue;
                Buffer.BlockCopy(PixelData, srcOffset, pixelsForBmp, dstOffset, (PixelFormat == PixelFormat.Format4bppIndexed ? bmpData.Width / 2 : bmpData.Width * bytesPerPixel));
            }

            Marshal.Copy(pixelsForBmp, 0, bmpData.Scan0, pixelsForBmp.Length);
            texture.UnlockBits(bmpData);

            if (palette != null)
            {
                ColorPalette texturePalette = texture.Palette;
                for (int j = 0; j < palette.Length; j++) texturePalette.Entries[j] = palette[j];
                texture.Palette = texturePalette;
            }

            Bitmap realTexture = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(realTexture))
            {
                g.DrawImageUnscaled(texture, 0, 0);
            }

            return realTexture;
        }
    }
}
