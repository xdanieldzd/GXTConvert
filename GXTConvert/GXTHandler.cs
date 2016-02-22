using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using GXTConvert.Exceptions;

namespace GXTConvert
{
    // http://www.vitadevwiki.com/index.php?title=GXT
    // https://github.com/vitasdk/vita-headers/

    // TODO: prettier BUV handling? also, is BUV common or only used in SAO?

    public class SceGxtHeader
    {
        public const string ExpectedMagicNumber = "GXT\0";

        public string MagicNumber { get; private set; }
        public uint Version { get; private set; } // TODO: 0x10000003 == 3.01 ???
        public uint NumTextures { get; private set; }
        public uint TextureDataOffset { get; private set; }
        public uint TextureDataSize { get; private set; }
        public uint NumP4Palettes { get; private set; }
        public uint NumP8Palettes { get; private set; }
        public uint Padding { get; private set; }

        public SceGxtHeader(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (MagicNumber != ExpectedMagicNumber)
                throw new UnknownMagicException(string.Format("Unexpected '{0}' for GTX", new string(MagicNumber.Where(x => !Char.IsControl(x)).ToArray()).TrimEnd('\0')));

            Version = reader.ReadUInt32();
            NumTextures = reader.ReadUInt32();
            TextureDataOffset = reader.ReadUInt32();
            TextureDataSize = reader.ReadUInt32();
            NumP4Palettes = reader.ReadUInt32();
            NumP8Palettes = reader.ReadUInt32();
            Padding = reader.ReadUInt32();
        }
    }

    public class SceGxtTextureInfo
    {
        public uint DataOffset { get; private set; }
        public uint DataSize { get; private set; }
        public int PaletteIndex { get; private set; }
        public uint Flags { get; private set; }
        public SceGxmTextureType TextureType { get; private set; }
        public SceGxmTextureFormat TextureFormat { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort NumMipmaps { get; private set; }
        public ushort Unknown0x3E { get; private set; }
        //TODO: where's byteStride for texture type LinearStrided?

        public SceGxmTextureBaseFormat TextureBaseFormat { get { return (SceGxmTextureBaseFormat)((uint)TextureFormat & 0xFFFF0000); } }

        public SceGxtTextureInfo(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            PaletteIndex = reader.ReadInt32();
            Flags = reader.ReadUInt32();
            TextureType = (SceGxmTextureType)reader.ReadUInt32();
            TextureFormat = (SceGxmTextureFormat)reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            NumMipmaps = reader.ReadUInt16();
            Unknown0x3E = reader.ReadUInt16();
        }
    }

    // TODO: verify me!
    public class BUVChunk
    {
        public const string ExpectedMagicNumber = "BUV\0";

        public string MagicNumber { get; private set; }
        public uint NumEntries { get; private set; }

        public BUVEntry[] Entries { get; private set; }

        public BUVChunk(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (MagicNumber != ExpectedMagicNumber)
                throw new UnknownMagicException(string.Format("Unexpected '{0}' for BUV", new string(MagicNumber.Where(x => !Char.IsControl(x)).ToArray()).TrimEnd('\0')));

            NumEntries = reader.ReadUInt32();

            Entries = new BUVEntry[NumEntries];
            for (int i = 0; i < Entries.Length; i++) Entries[i] = new BUVEntry(stream);
        }
    }

    // TODO: verify me!
    public class BUVEntry
    {
        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public short PaletteIndex { get; private set; }
        public ushort Unknown0x0A { get; private set; }

        public BUVEntry(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PaletteIndex = reader.ReadInt16();
            Unknown0x0A = reader.ReadUInt16();
        }
    }

    public class GXTHandler
    {
        public SceGxtHeader Header { get; private set; }
        public SceGxtTextureInfo[] TextureInfos { get; private set; }

        public BUVChunk BUVChunk { get; private set; }

        public Color[][] P4Palettes { get; private set; }
        public Color[][] P8Palettes { get; private set; }
        public Bitmap[] Textures { get; private set; }

        public Bitmap[] BUVTextures { get; private set; }

        Bitmap[][] allPaletteTextures;

        public GXTHandler(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            Header = new SceGxtHeader(stream);

            TextureInfos = new SceGxtTextureInfo[Header.NumTextures];
            for (int i = 0; i < TextureInfos.Length; i++) TextureInfos[i] = new SceGxtTextureInfo(stream);

            // TODO: any other way to detect these?
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == BUVChunk.ExpectedMagicNumber)
            {
                stream.Seek(-4, SeekOrigin.Current);
                BUVChunk = new BUVChunk(stream);
            }

            long paletteOffset = stream.Length - (((Header.NumP8Palettes * 256) * 4) + ((Header.NumP4Palettes * 16) * 4));
            stream.Seek(paletteOffset, SeekOrigin.Begin);

            P4Palettes = new Color[Header.NumP4Palettes][];
            for (int i = 0; i < P4Palettes.Length; i++) P4Palettes[i] = ReadPalette(reader, 16);

            P8Palettes = new Color[Header.NumP8Palettes][];
            for (int i = 0; i < P8Palettes.Length; i++) P8Palettes[i] = ReadPalette(reader, 256);

            if (BUVChunk != null)
                allPaletteTextures = new Bitmap[TextureInfos.Length][];

            Textures = new Bitmap[TextureInfos.Length];
            for (int i = 0; i < TextureInfos.Length; i++)
            {
                SceGxtTextureInfo info = TextureInfos[i];
                reader.BaseStream.Seek(info.DataOffset, SeekOrigin.Begin);

                PixelFormat pixelFormat;
                byte[] pixelData = null;
                bool needPostProcess = true;

                switch (info.TextureFormat)
                {
                    case SceGxmTextureFormat.U8U8U8U8_ARGB:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.U8U8U8_RGB:
                        pixelFormat = PixelFormat.Format24bppRgb;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.U1U5U5U5_ARGB:
                        pixelFormat = PixelFormat.Format16bppArgb1555;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.U5U6U5_RGB:
                        // TODO: verify me!
                        pixelFormat = PixelFormat.Format16bppRgb565;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.U8U8U8X8_RGB1:
                        // TODO: verify me!
                        pixelFormat = PixelFormat.Format32bppRgb;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.P4_ABGR:
                        pixelFormat = PixelFormat.Format4bppIndexed;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.P8_ABGR:
                        pixelFormat = PixelFormat.Format8bppIndexed;
                        pixelData = reader.ReadBytes((int)info.DataSize);
                        break;

                    case SceGxmTextureFormat.UBC1_ABGR:
                    case SceGxmTextureFormat.UBC2_ABGR:
                    case SceGxmTextureFormat.UBC3_ABGR:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        pixelData = Compression.DXTx.Decompress(reader, info);
                        break;

                    case SceGxmTextureFormat.PVRT2BPP_ABGR:
                    case SceGxmTextureFormat.PVRT2BPP_1BGR:
                    case SceGxmTextureFormat.PVRT4BPP_ABGR:
                    case SceGxmTextureFormat.PVRT4BPP_1BGR:
                        needPostProcess = false;
                        pixelFormat = PixelFormat.Format32bppArgb;
                        pixelData = Compression.PVRTC.Decompress(reader, info);
                        break;

                    case SceGxmTextureFormat.U8_1RRR:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        pixelData = new byte[info.DataSize * 4];
                        for (int j = 0; j < pixelData.Length; j += 4)
                        {
                            pixelData[j + 0] = pixelData[j + 1] = pixelData[j + 2] = reader.ReadByte();
                            pixelData[j + 3] = 0xFF;
                        }
                        break;

                    default:
                        throw new FormatNotImplementedException(info.TextureFormat);
                }

                // TODO: is this right? PVRTC doesn't need this, but everything else does?
                if (needPostProcess)
                {
                    switch (info.TextureType)
                    {
                        case SceGxmTextureType.Linear:
                            // Nothing to be done!
                            break;

                        case SceGxmTextureType.Tiled:
                            // TODO: verify me!
                            pixelData = PostProcessing.UntileTexture(pixelData, info.Width, info.Height, pixelFormat);
                            break;

                        case SceGxmTextureType.Swizzled:
                            pixelData = PostProcessing.UnswizzleTexture(pixelData, info.Width, info.Height, pixelFormat);
                            break;

                        default:
                            throw new TypeNotImplementedException(info.TextureType);
                    }
                }

                Textures[i] = CreateTexture(info, pixelFormat, pixelData, info.PaletteIndex);

                // TODO: make this less kludge-like?
                if (BUVChunk != null && allPaletteTextures != null)
                {
                    Color[][] palettes;
                    switch (info.TextureBaseFormat)
                    {
                        case SceGxmTextureBaseFormat.P4: palettes = P4Palettes; break;
                        case SceGxmTextureBaseFormat.P8: palettes = P8Palettes; break;
                        default: throw new PaletteNotImplementedException(info.TextureBaseFormat);
                    }

                    allPaletteTextures[i] = new Bitmap[palettes.Length];
                    for (int j = 0; j < allPaletteTextures[i].Length; j++)
                        allPaletteTextures[i][j] = CreateTexture(info, pixelFormat, pixelData, j);
                }
            }

            if (BUVChunk != null)
            {
                BUVTextures = new Bitmap[BUVChunk.Entries.Length];
                for (int i = 0; i < BUVTextures.Length; i++)
                {
                    BUVEntry entry = BUVChunk.Entries[i];

                    // TODO: is it always texture 0?
                    Bitmap sourceImage;

                    if (entry.PaletteIndex != -1)
                        sourceImage = allPaletteTextures[0][entry.PaletteIndex];
                    else
                        sourceImage = Textures[0];

                    BUVTextures[i] = sourceImage.Clone(new Rectangle(entry.X, entry.Y, entry.Width, entry.Height), sourceImage.PixelFormat);
                }
            }
        }

        private Color[] ReadPalette(BinaryReader reader, int numColor)
        {
            Color[] palette = new Color[numColor];
            byte r, g, b, a;
            for (int i = 0; i < palette.Length; i++)
            {
                r = reader.ReadByte(); g = reader.ReadByte(); b = reader.ReadByte(); a = reader.ReadByte();
                palette[i] = Color.FromArgb(a, r, g, b);
            }
            return palette;
        }

        private Bitmap CreateTexture(SceGxtTextureInfo info, PixelFormat pixelFormat, byte[] pixelData, int paletteIndex = -1)
        {
            Bitmap texture = new Bitmap(info.Width, info.Height, pixelFormat);
            BitmapData bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadWrite, texture.PixelFormat);

            byte[] pixelsForBmp = new byte[bmpData.Height * bmpData.Stride];
            int bytesPerPixel = (Bitmap.GetPixelFormatSize(pixelFormat) / 8);
            for (int y = 0; y < bmpData.Height; y++)
                Buffer.BlockCopy(pixelData, y * bmpData.Width * bytesPerPixel, pixelsForBmp, y * bmpData.Stride, bmpData.Width * bytesPerPixel);

            Marshal.Copy(pixelsForBmp, 0, bmpData.Scan0, pixelsForBmp.Length);
            texture.UnlockBits(bmpData);

            if (paletteIndex != -1)
            {
                Color[] palette;
                switch (info.TextureBaseFormat)
                {
                    case SceGxmTextureBaseFormat.P4: palette = P4Palettes[paletteIndex]; break;
                    case SceGxmTextureBaseFormat.P8: palette = P8Palettes[paletteIndex]; break;
                    default: throw new PaletteNotImplementedException(info.TextureBaseFormat);
                }

                ColorPalette texturePalette = texture.Palette;
                for (int j = 0; j < palette.Length; j++) texturePalette.Entries[j] = palette[j];
                texture.Palette = texturePalette;
            }

            return texture;
        }
    }
}
